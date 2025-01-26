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

    [Function(nameof(DoFinalisePaymentsActivity))]
    public async Task<string> DoFinalisePaymentsActivity([ActivityTrigger] string tenantToProcess, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoFinalisePaymentsActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(logger, tenantToProcess, cn);
            if (tenant != null) 
            {
                logger.LogInformation($"****** Started {nameof(DoFinalisePaymentsActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    await FinaliseOnlinePayment.Process(tenant, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoFinalisePaymentsActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Connection string is null"); }
        return $"{nameof(DoFinalisePaymentsActivity)} completed.";
    }

    [Function("DoFinalisePayments")]
    public static async Task<HttpResponseData> DoFinalisePayments(
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

