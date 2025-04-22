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

    [Function(nameof(DoDatabaseCleanupActivity))]
    public async Task<string> DoDatabaseCleanupActivity([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                Log.Information("****** Started {activity} for {identifier}: {name}. ******",
                                        nameof(DoDatabaseCleanupActivity),
                                        tenant.Identifier,
                                        tenant.Name);
                try
                {
                    await LogStartTime(tenant);
                    await DatabaseCleanup.Process(tenant, cn);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing {nameof(DoDatabaseCleanupActivity)} for {tenant.Identifier}");
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

