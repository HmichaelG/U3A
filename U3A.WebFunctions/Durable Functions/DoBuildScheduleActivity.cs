using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.WebFunctions.Procedures;
using System.Text.Json;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.IdentityModel.Tokens.Jwt;
using Serilog.Core;
using System.Reflection;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{
    [Function(nameof(DoBuildScheduleActivity))]
    public async Task<string> DoBuildScheduleActivity([ActivityTrigger] string tenantToProcess, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoBuildScheduleActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenants(logger, tenantToProcess, cn);
            if (tenant != null)
            {
                logger.LogInformation($"****** Started {nameof(DoBuildScheduleActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    using (var dbc = new U3ADbContext(tenant))
                    {
                        dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                        using (var dbcT = new TenantDbContext(cn!))
                        {
                            await BusinessRule.BuildScheduleAsync(dbc, dbcT, tenant.Identifier!);
                        }
                        logger.LogInformation($"Class Schedule cache created for: {tenant.Identifier}.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoBuildScheduleActivity)} for {tenant.Identifier}");
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
        var options = new U3AFunctionOptions(req)
        {
            DurableActivity = DurableActivity.DoBuildSchedule
        };
        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }

}