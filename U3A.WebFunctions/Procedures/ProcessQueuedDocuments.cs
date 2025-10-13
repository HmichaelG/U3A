using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.WebFunctions.Procedures
{
    public static class ProcessQueuedDocuments
    {

        public static async Task Process(TenantInfo tenant, U3AFunctionOptions options)
        {
            int count = 0;
            List<DocumentQueue> queueItems = [];
            List<ExportData> exportData = [];
            using U3ADbContext dbc = new(tenant);
            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
            DateTime today = await Common.GetTodayAsync(dbc);
            DocumentTemplate? documentTemplate;
            if (dbc.DocumentQueue.Any(x => x.Status == DocumentQueueStatus.ReadyToSend))
            {
                DocumentServer server = new(dbc);
                string subject = string.Empty;
                queueItems = options.SendMailIdsToProcess.Count > 0
                    ? dbc.DocumentQueue
                        .Include(x => x.DocumentAttachments)
                        .AsEnumerable()
                        .Where(x => options.SendMailIdsToProcess.Contains(x.ID))
                        .ToList()
                    : await dbc.DocumentQueue
                        .Where(x => x.Status == DocumentQueueStatus.ReadyToSend)
                        .Include(x => x.DocumentAttachments).ToListAsync();
                foreach (DocumentQueue queueItem in queueItems)
                {
                    if (DateTime.UtcNow > queueItem.CreatedOn!.Value.AddHours(queueItem.DelayInHours))
                    {
                        queueItem.Status = DocumentQueueStatus.InProcess;
                        int result = await dbc.SaveChangesAsync();
                        try
                        {
                            documentTemplate = JsonSerializer.Deserialize<DocumentTemplate>(queueItem.DocumentTemplateJSON);
                            if (documentTemplate != null)
                            {
                                subject = documentTemplate.Subject!;
                                exportData = [];

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
                                    exportData = JsonSerializer.Deserialize<List<ExportData>>(queueItem.ExportDataJSON) ?? [];
                                }
                                count += exportData.Count;

                                // Get the attachments, if any
                                if (queueItem.DocumentAttachments != null)
                                {
                                    documentTemplate!.AttachmentBytes = [];
                                    foreach (DocumentQueueAttachment attachment in queueItem.DocumentAttachments)
                                    {
                                        documentTemplate.AttachmentBytes.Add(attachment.Attachment);
                                    }
                                }

                                //and, process...
                                int countSent = queueItem.SendToMultipleRecipients
                                    ? await server.SendQueuedEmailToMultipleRecipientsAsync(documentTemplate!, exportData!, queueItem.OverrideCommunicationPreference)
                                    : await server.SendQueuedEmailToSingleRecipientAsync(documentTemplate!, exportData!, queueItem.OverrideCommunicationPreference);
                                queueItem.EmailCount = countSent;
                                queueItem.Status = DocumentQueueStatus.Complete;
                                queueItem.Result = "Ok";
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error Processing: {subject}");
                            queueItem.Status = DocumentQueueStatus.Error;
                            queueItem.Result = "Processing Error";
                        }
                        finally
                        {
                            result = await dbc.SaveChangesAsync();
                        }
                    }
                }
                Log.Information($"{count} queued documents sent.");
            }
            else { Log.Information("There were no queued documents to send."); }

            // Delete expired records
            int daysToKeep = 14;
            dbc.Database.SetCommandTimeout(60 * 3);
            int deleted = dbc.Database.ExecuteSql($"DELETE FROM DocumentQueue WHERE DATEDIFF(DAY, CreatedOn, GETDATE()) > {daysToKeep}");
            Log.Information($"Deleted {deleted} document queue records because they are more than {daysToKeep} days old.");
        }
    }
}
