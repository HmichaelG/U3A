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
    [Function(nameof(DoCreateUpdateLeaderHistory))]
    public async Task<string> DoCreateUpdateLeaderHistory([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                Log.Information($"****** Started {nameof(DoCreateUpdateLeaderHistory)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(tenant);
                    using (var dbc = new U3ADbContext(tenant))
                    {
                        dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                        using (var dbcT = new TenantDbContext(cn!))
                        {
                            await BusinessRule.CreateUpdateLeaderHistoryAsync(dbc);
                        }
                        Log.Information($"Leader History created for: {tenant.Identifier}.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing {nameof(DoCreateUpdateLeaderHistory)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Connection string is null"); }
        return $"{nameof(DoCreateUpdateLeaderHistory)} completed.";
    }

}