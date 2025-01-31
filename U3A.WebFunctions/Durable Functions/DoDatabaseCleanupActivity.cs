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

    [Function(nameof(DoDatabaseCleanupActivity))]
    public async Task<string> DoDatabaseCleanupActivity([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoDatabaseCleanupActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(logger, options.TenantIdentifier, cn);
            if (tenant != null) 
            {
                logger.LogInformation($"****** Started {nameof(DoDatabaseCleanupActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    await DatabaseCleanup.Process(tenant, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoDatabaseCleanupActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Database connection string is null"); }
        return $"{nameof(DoDatabaseCleanupActivity)} completed.";
    }

    [Function("DoDatabaseCleanup")]
    public static async Task<HttpResponseData> DoDatabaseCleanup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var options = new U3AFunctionOptions(req)
        {
            DurableActivity = DurableActivity.DoDatabaseCleanup
        };
        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }
}

