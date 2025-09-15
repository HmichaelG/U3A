using Microsoft.EntityFrameworkCore;
using Serilog;
using U3A.Database;
using U3A.Model;

namespace U3A.WebFunctions.Procedures;

public static class DatabaseCleanup
{
    public static async Task Process(TenantInfo tenant, string connectionString)
    {
        using (var dbc = new U3ADbContext(tenant))
        {
            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
            try
            {
                _ = await dbc.Database.ExecuteSqlRawAsync(@"execute [dbo].[prcDbCleanup]");
                Log.Information($"[{tenant.Identifier}]: Execute [dbo].[prcDbCleanup] completed.");
            }
            catch (Exception ex)
            {
                Log.Error("[{tenant.Identifier}]: Execute [dbo].[prcDbCleanup] failed: " + ex.Message);
            }
        }
        using (var dbc = new TenantDbContext(connectionString))
        {
            var cutOff = DateTime.UtcNow.Date.AddDays(-3);
            var tableName = "LogAutoEnrol";
            await deleteLogs(tenant, dbc, cutOff, tableName);
            tableName = "LogEvents";
            await deleteLogs(tenant, dbc, cutOff, tableName);
        }
    }
    static async Task deleteLogs(TenantInfo tenant, TenantDbContext dbc, DateTime cutOff, string tableName)
    {
        var total = 0;
        try
        {
            while (true)
            {
                var cmd = $"delete top (5000) from [dbo].[{tableName}] where timestamp < '{cutOff.ToString("dd-MMMM-yyyy")}'";
                var count = await dbc.Database.ExecuteSqlRawAsync(cmd);
                total += count;
                Log.Information("[{identifier}]: {total} records deleted from [{tableName}]",
                                    tenant.Identifier,
                                    total,
                                    tableName);
                if (count <= 0) { break; }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[{tenant.Identifier}]: Delete from [{tableName}] failed: {ex.Message}");
        }
    }
}

