using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Serilog;
using U3A.Model;
using U3A.WebFunctions.Procedures;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{
    [Function(nameof(DoProcessQueuedDocumentsActivity))]
    public async Task<string> DoProcessQueuedDocumentsActivity([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        string? cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            TenantInfo? tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                Log.Information($"****** Started {nameof(DoProcessQueuedDocumentsActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(tenant);
                    bool isBackgroundProcessingEnabled = !await Common.isBackgroundProcessingDisabled(tenant);
                    if (isBackgroundProcessingEnabled)
                    {
                        await ProcessQueuedDocuments.Process(tenant, options);
                    }
                    else
                    {
                        Log.Information($"[{tenant.Identifier}]: Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing {nameof(DoProcessQueuedDocumentsActivity)} for {tenant.Identifier}");
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
        U3AFunctionOptions options = new(req)
        {
            DurableActivity = DurableActivity.DoProcessQueuedDocuments
        };
        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }


}


