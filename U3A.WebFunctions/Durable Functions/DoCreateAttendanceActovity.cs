using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.WebFunctions.Procedures;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{

    [Function(nameof(DoCreateAttendanceActivity))]
    public async Task<string> DoCreateAttendanceActivity([ActivityTrigger] string tenantToProcess, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoCreateAttendanceActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            foreach (var tenant in GetTenants(logger, tenantToProcess, cn))
            {
                logger.LogInformation($"****** Started {nameof(DoCreateAttendanceActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    var isBackgroundProcessingEnabled = !await Common.isBackgroundProcessingDisabled(tenant);
                    if (isBackgroundProcessingEnabled)
                    {
                        await CreateAttendance.Process(tenant, logger);
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
        ILogger logger = executionContext.GetLogger("DoCreateAttendance");
        var options = new U3AFunctionOptions(req) { DoCreateAttendance = true };
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(DurableFunctions), options);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response with an instance management payload.
        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
}

