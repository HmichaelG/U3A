using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.WebFunctions.Procedures;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{

    [Function(nameof(DoBringForwardEnrolmentsActivity))]
    public async Task<string> DoBringForwardEnrolmentsActivity([ActivityTrigger] string tenantToProcess, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoBringForwardEnrolmentsActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            foreach (var tenant in GetTenants(logger, tenantToProcess, cn))
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
        ILogger logger = executionContext.GetLogger("DoBringForwardEnrolments");
        var options = new U3AFunctionOptions(req) { DoBringForwardEnrolments = true, DoCorrespondence = true };
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(DurableFunctions), options);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response with an instance management payload.
        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
}

