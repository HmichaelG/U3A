using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.Diagnostics;
using System.Text.Json;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.WebFunctions.Procedures
{
    public static class ProcessQueuedDocuments
    {

        public static async Task Process(TenantInfo tenant, U3AFunctionOptions options, ILogger logger)
        {
            int count = 0;
            List<DocumentQueue> queueItems = new();
            List<ExportData> exportData = new();
            using (var dbc = new U3ADbContext(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                var today = await Common.GetTodayAsync(dbc);
                DocumentTemplate? documentTemplate;
                if (dbc.DocumentQueue.Any(x => x.Status == DocumentQueueStatus.ReadyToSend))
                {
                    var server = new DocumentServer(dbc);
                    string subject = string.Empty;
                    if (options.SendMailIdsToProcess.Count > 0)
                    {
                        queueItems = dbc.DocumentQueue
                            .Include(x => x.DocumentAttachments)
                            .AsEnumerable()
                            .Where(x => options.SendMailIdsToProcess.Contains(x.ID))
                            .ToList();
                    }
                    else
                    {
                        queueItems = await dbc.DocumentQueue
                            .Where(x => x.Status == DocumentQueueStatus.ReadyToSend)
                            .Include(x => x.DocumentAttachments).ToListAsync();
                    }
                    foreach (var queueItem in queueItems)
                    {
                        if (DateTime.UtcNow > queueItem.CreatedOn!.Value.AddHours(queueItem.DelayInHours))
                        {
                            queueItem.Status = DocumentQueueStatus.InProcess;
                            var result = await dbc.SaveChangesAsync();
                            try
                            {
                                documentTemplate = JsonSerializer.Deserialize<DocumentTemplate>(queueItem.DocumentTemplateJSON);
                                if (documentTemplate != null)
                                {
                                    subject = documentTemplate.Subject!;
                                    exportData = new();

                                    // get members who are to receive the document
                                    if (!string.IsNullOrWhiteSpace(queueItem.MemberIdToExport))
                                    {
                                        List<Guid>? personIDsToExport = JsonSerializer.Deserialize<List<Guid>>(queueItem.MemberIdToExport);
                                        if (personIDsToExport != null)
                                        {
                                            exportData = await BusinessRule.GetExportDataAsync(dbc, personIDsToExport);
                                        }
                                    }
                                    else
                                    {
                                        //Obsolete: backwards compatibility only
                                        exportData = JsonSerializer.Deserialize<List<ExportData>>(queueItem.ExportDataJSON) ?? new();
                                    }
                                    count += exportData.Count;

                                    // Get the attachments, if any
                                    if (queueItem.DocumentAttachments != null)
                                    {
                                        documentTemplate!.AttachmentBytes = new List<byte[]>();
                                        foreach (var attachment in queueItem.DocumentAttachments)
                                        {
                                            documentTemplate.AttachmentBytes.Add(attachment.Attachment);
                                        }
                                    }

                                    //and, process...
                                    var countSent = 0;
                                    if (queueItem.SendToMultipleRecipients)
                                    {
                                        countSent = await server.SendQueuedEmailToMultipleRecipientsAsync(documentTemplate!, exportData!, queueItem.OverrideCommunicationPreference);
                                    }
                                    else
                                    {
                                       countSent = await server.SendQueuedEmailToSingleRecipientAsync(documentTemplate!, exportData!, queueItem.OverrideCommunicationPreference);
                                    }
                                    queueItem.EmailCount = countSent;
                                    queueItem.Status = DocumentQueueStatus.Complete;
                                    queueItem.Result = "Ok";
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"Error Processing: {subject}");
                                queueItem.Status = DocumentQueueStatus.Error;
                                queueItem.Result = "Processing Error";
                            }
                            finally
                            {
                                result = await dbc.SaveChangesAsync();
                            }
                        }
                    }
                    logger.LogInformation($"{count} queued documents sent.");
                }
                else { logger.LogInformation("There were no queued documents to send."); }

                // Delete expired records
                int daysToKeep = 14;
                dbc.Database.SetCommandTimeout(60 * 3);
                var deleted = dbc.Database.ExecuteSql($"DELETE FROM DocumentQueue WHERE DATEDIFF(DAY, CreatedOn, GETDATE()) > {daysToKeep}");
                logger.LogInformation($"Deleted {deleted} document queue records because they are more than {daysToKeep} days old.");
            }
        }
    }
}
