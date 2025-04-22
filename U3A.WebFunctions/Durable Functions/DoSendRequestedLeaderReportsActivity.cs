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
    [Function(nameof(DoSendRequestedLeaderReportsActivity))]
    public async Task<string> DoSendRequestedLeaderReportsActivity([ActivityTrigger]
                        U3AFunctionOptions options,
                        FunctionContext executionContext)
    {
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                Log.Information($"****** Started {nameof(DoSendRequestedLeaderReportsActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(tenant);
                    var isBackgroundProcessingEnabled = !(await Common.isBackgroundProcessingDisabled(tenant));
                    if (isBackgroundProcessingEnabled)
                    {
                        await ProcessCorrespondence.Process(tenant, cn!, options);
                    }
                    else
                    {
                        Log.Information($"[{tenant.Identifier}]: Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing {nameof(DoSendRequestedLeaderReportsActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Database Connection string is null"); }
        return $"{nameof(DoSendRequestedLeaderReportsActivity)} completed.";
    }

    [Function("DoSendRequestedLeaderReports")]
    public static async Task<HttpResponseData> DoSendRequestedLeaderReports(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var options = new U3AFunctionOptions(req)
        {
            DurableActivity = DurableActivity.DoSendRequestedLeaderReports
        };
        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }

}