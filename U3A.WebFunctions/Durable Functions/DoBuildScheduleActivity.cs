using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Serilog;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{
    [Function(nameof(DoBuildScheduleActivity))]
    public async Task<string> DoBuildScheduleActivity([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        string? cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            TenantInfo? tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                Log.Information($"****** Started {nameof(DoBuildScheduleActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(tenant);
                    using U3ADbContext dbc = new(tenant);
                    dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                    using (TenantDbContext dbcT = new(cn!))
                    {
                        await BusinessRule.BuildScheduleAsync(dbc, dbcT, tenant.Identifier!);
                    }
                    Log.Information($"Class Schedule cache created for: {tenant.Identifier}.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing {nameof(DoBuildScheduleActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Connection string is null"); }
        return $"{nameof(DoBuildScheduleActivity)} completed.";
    }



    [Function("DoBuildSchedule")]
    public static async Task<HttpResponseData> DoBuildSchedule(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext executionContext)
    {
        U3AFunctionOptions options = new(req)
        {
            DurableActivity = DurableActivity.DoBuildSchedule
        };
        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }

}