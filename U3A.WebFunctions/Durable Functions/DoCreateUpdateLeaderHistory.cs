using Microsoft.Azure.Functions.Worker;
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
        string? cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            TenantInfo? tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                Log.Information($"****** Started {nameof(DoCreateUpdateLeaderHistory)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(tenant);
                    using U3ADbContext dbc = new(tenant);
                    dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                    using (TenantDbContext dbcT = new(cn!))
                    {
                        await BusinessRule.CreateUpdateLeaderHistoryAsync(dbc);
                    }
                    Log.Information($"Leader History created for: {tenant.Identifier}.");
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