using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Serilog;
using U3A.Model;
using U3A.WebFunctions.Procedures;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{

    [Function(nameof(DoLeaderReportsAtTermStartActivity))]
    public async Task<string> DoLeaderReportsAtTermStartActivity([ActivityTrigger] string tenantToProcess, FunctionContext executionContext)
    {
        string? cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            TenantInfo? tenant = GetTenant(tenantToProcess, cn);
            if (tenant != null)
            {
                Log.Information($"****** Started {nameof(DoLeaderReportsAtTermStartActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(tenant);
                    bool isBackgroundProcessingEnabled = !await Common.isBackgroundProcessingDisabled(tenant);
                    if (isBackgroundProcessingEnabled)
                    {
                        await SendLeaderReportsAtTermStart.Process(tenant);
                    }
                    else
                    {
                        Log.Information($"[{tenant.Identifier}]: Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing {nameof(DoLeaderReportsAtTermStartActivity)} for {tenant.Identifier}");
                }
            }
        }
        else { throw new NullReferenceException("Database connection string is null"); }
        return $"{nameof(DoLeaderReportsAtTermStartActivity)} completed.";
    }

}


