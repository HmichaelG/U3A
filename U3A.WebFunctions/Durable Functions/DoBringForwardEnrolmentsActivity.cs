using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using U3A.Model;
using U3A.WebFunctions.Procedures;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{

    [Function(nameof(DoBringForwardEnrolmentsActivity))]
    public async Task<string> DoBringForwardEnrolmentsActivity([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoBringForwardEnrolmentsActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                logger.LogInformation($"****** Started {nameof(DoBringForwardEnrolmentsActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    await BringForwardEnrolments.Process(tenant, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoBringForwardEnrolmentsActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Database connection string is null"); }
        return $"{nameof(DoBringForwardEnrolmentsActivity)} completed.";
    }

    [Function("DoBringForwardEnrolments")]
    public static async Task<HttpResponseData> DoBringForwardEnrolments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var options = new U3AFunctionOptions(req)
        {
            DurableActivity = DurableActivity.DoBringForwardEnrolments
        };
        return await ScheduleFunctionAsync(client,
                                executionContext,
                                req,
                                options,
                                WaitForCompletion: false);
    }
}

