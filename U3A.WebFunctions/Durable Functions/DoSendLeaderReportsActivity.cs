using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.WebFunctions.Procedures;
using U3A.Model;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{

    [Function(nameof(DoSendLeaderReportsActivity))]
    public async Task<string> DoSendLeaderReportsActivity([ActivityTrigger] string tenantToProcess, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoSendLeaderReportsActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(logger, tenantToProcess, cn);
            if (tenant != null)
            {
                logger.LogInformation($"****** Started {nameof(DoSendLeaderReportsActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    var isBackgroundProcessingEnabled = !(await Common.isBackgroundProcessingDisabled(tenant));
                    if (isBackgroundProcessingEnabled)
                    {
                        await SendLeaderReports.Process(tenant, logger);
                    }
                    else
                    {
                        logger.LogInformation($"[{tenant.Identifier}]: Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoSendLeaderReportsActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Database connection string is null"); }
        return $"{nameof(DoSendLeaderReportsActivity)} completed.";
    }

    [Function("DoSendLeaderReports")]
    public static async Task<HttpResponseData> DoSendLeaderReports(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var options = new U3AFunctionOptions(req)
        {
            DurableActivity = DurableActivity.DoSendLeaderReports
        };
        return await ScheduleFunctionAsync(client,
                                executionContext,
                                req,
                                options,
                                WaitForCompletion: false);
    }
}


