using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.WebFunctions.Procedures;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using U3A.Model;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{
    [Function(nameof(DoSendRequestedLeaderReportsActivity))]
    public async Task<string> DoSendRequestedLeaderReportsActivity([ActivityTrigger]
                        U3AFunctionOptions options,
                        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoSendRequestedLeaderReportsActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                logger.LogInformation($"****** Started {nameof(DoSendRequestedLeaderReportsActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    var isBackgroundProcessingEnabled = !(await Common.isBackgroundProcessingDisabled(tenant));
                    if (isBackgroundProcessingEnabled)
                    {
                        await ProcessCorrespondence.Process(tenant, cn!, options, logger);
                    }
                    else
                    {
                        logger.LogInformation($"[{tenant.Identifier}]: Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoSendRequestedLeaderReportsActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Database Connection string is null"); }
        return $"{nameof(DoSendRequestedLeaderReportsActivity)} completed.";
    }

    [Function("DoSendRequestedLeaderReports")]
    public static async Task<HttpResponseData> DoSendRequestedLeaderReports(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var options = new U3AFunctionOptions(req)
        {
            DurableActivity = DurableActivity.DoSendRequestedLeaderReports
        };
        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }

}