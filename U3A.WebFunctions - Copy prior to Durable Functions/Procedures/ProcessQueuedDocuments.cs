using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.WebFunctions.Procedures
{
    public static class ProcessQueuedDocuments
    {
        public static async Task Process(TenantInfo tenant, ILogger logger)
        {
            using (var dbc = new U3ADbContext(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                var today = await Common.GetTodayAsync(dbc);
                DocumentTemplate? documentTemplate;
                if (dbc.DocumentQueue.Any(x => x.Status == DocumentQueueStatus.ReadyToSend))
                {
                    var server = new DocumentServer(dbc);
                    string subject = string.Empty;
                    foreach (var queueItem in await dbc.DocumentQueue
                                    .Where(x => x.Status == DocumentQueueStatus.ReadyToSend)
                                    .Include(x => x.DocumentAttachments).ToListAsync())
                    {
                        queueItem.Status = DocumentQueueStatus.InProcess;
                        var result = await dbc.SaveChangesAsync();
                        try
                        {
                            documentTemplate = JsonSerializer.Deserialize<DocumentTemplate>(queueItem.DocumentTemplateJSON);
                            if (documentTemplate != null)
                            {
                                subject = documentTemplate.Subject!;
                                List<ExportData>? exportData = JsonSerializer.Deserialize<List<ExportData>>(queueItem.ExportDataJSON);
                                if (queueItem.DocumentAttachments != null)
                                {
                                    documentTemplate!.AttachmentBytes = new List<byte[]>();
                                    foreach (var attachment in queueItem.DocumentAttachments)
                                    {
                                        documentTemplate.AttachmentBytes.Add(attachment.Attachment);
                                    }
                                }
                                if (queueItem.SendToMultipleRecipients)
                                {
                                    await server.SendQueuedEmailToMultipleRecipientsAsync(documentTemplate!, exportData!, queueItem.OverrideCommunicationPreference);
                                }
                                else
                                {
                                    await server.SendQueuedEmailToSingleRecipientAsync(documentTemplate!, exportData!, queueItem.OverrideCommunicationPreference);
                                }
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
                    logger.LogInformation($"Queued Documents: {server.BatchCount} Batches, {server.BatchSuccessCount} Accepted, {server.BatchFailureCount} Failures.");
                }
                else { logger.LogInformation("There were no queued documents to send."); }

                // Delete expired records
                dbc.RemoveRange(dbc.DocumentQueue.AsEnumerable()
                    .Where(x => (today - x.CreatedOn.GetValueOrDefault()).Days > 7));
                var deleted = dbc.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted).Count();
                if (deleted > 0)
                {
                    var result = await dbc.SaveChangesAsync();
                    logger.LogInformation($"Deleted {deleted} document queue records because they are more than 7 days old.");
                }
            }
        }
    }
}
