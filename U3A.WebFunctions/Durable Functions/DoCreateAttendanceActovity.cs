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

    [Function(nameof(DoCreateAttendanceActivity))]
    public async Task<string> DoCreateAttendanceActivity([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoCreateAttendanceActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                logger.LogInformation($"****** Started {nameof(DoCreateAttendanceActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    var isBackgroundProcessingEnabled = !(await Common.isBackgroundProcessingDisabled(tenant));
                    if (isBackgroundProcessingEnabled)
                    {
                        await CreateAttendance.Process(tenant);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoCreateAttendanceActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Database connection string is null"); }
        return $"{nameof(DoCreateAttendanceActivity)} completed.";
    }

    [Function("DoCreateAttendance")]
    public static async Task<HttpResponseData> DoCreateAttendance(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var options = new U3AFunctionOptions(req)
        {
            DurableActivity = DurableActivity.DoCreateAttendance
        };
        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }
}

