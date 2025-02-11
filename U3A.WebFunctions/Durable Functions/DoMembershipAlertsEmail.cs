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

    [Function(nameof(DoMembershipAlertsEmailActivity))]
    public async Task<string> DoMembershipAlertsEmailActivity([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoMembershipAlertsEmailActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null) 
            {
                logger.LogInformation($"****** Started {nameof(DoMembershipAlertsEmailActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    var isBackgroundProcessingEnabled = !(await Common.isBackgroundProcessingDisabled(tenant));
                    if (isBackgroundProcessingEnabled)
                    {
                        await ProcessMembershipAlertsEmail.Process(tenant, logger);
                    }
                    else
                    {
                        logger.LogInformation($"[{tenant.Identifier}]: Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoMembershipAlertsEmailActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Database connection string is null"); }
        return $"{nameof(DoMembershipAlertsEmailActivity)} completed.";
    }

    [Function("DoMembershipAlertsEmail")]
    public static async Task<HttpResponseData> DoMembershipAlertsEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var options = new U3AFunctionOptions(req)
        {
            DurableActivity = DurableActivity.DoFinalisePayments
        };
        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }
}

