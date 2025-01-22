using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.WebFunctions.Procedures;
using Microsoft.DurableTask;
using System.Net;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{

    [Function(nameof(DoProcessQueuedDocumentsActivity))]
    public async Task<string> DoProcessQueuedDocumentsActivity([ActivityTrigger] string tenantToProcess, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoProcessQueuedDocumentsActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            foreach (var tenant in GetTenants(logger, tenantToProcess, cn))
            {
                logger.LogInformation($"****** Started {nameof(DoProcessQueuedDocumentsActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    var isBackgroundProcessingEnabled = !await Common.isBackgroundProcessingDisabled(tenant);
                    if (isBackgroundProcessingEnabled)
                    {
                        await ProcessQueuedDocuments.Process(tenant, logger);
                    }
                    else
                    {
                        logger.LogInformation($"[{tenant.Identifier}]: Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoProcessQueuedDocumentsActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Database connection string is null"); }
        return $"{nameof(DoProcessQueuedDocumentsActivity)} completed.";
    }

    [Function("DoProcessQueuedDocuments")]
    public static async Task<HttpResponseData> DoProcessQueuedDocuments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var options = new U3AFunctionOptions(req) { DoProcessQueuedDocuments = true };
        return await ScheduleFunctionAsync(nameof(DoProcessQueuedDocuments), client, executionContext,req, options);
    }
}


