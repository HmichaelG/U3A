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

    [Function(nameof(DoFinalisePaymentsActivity))]
    public async Task<string> DoFinalisePaymentsActivity([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        string? cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            TenantInfo? tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                Log.Information($"****** Started {nameof(DoFinalisePaymentsActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(tenant);
                    await FinaliseOnlinePayment.Process(tenant);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing {nameof(DoFinalisePaymentsActivity)} for {tenant.Identifier}");
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
        U3AFunctionOptions options = new(req)
        {
            DurableActivity = DurableActivity.DoFinalisePayments
        };
        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }
}

