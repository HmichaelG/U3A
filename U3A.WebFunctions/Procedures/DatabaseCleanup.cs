using DevExpress.CodeParser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using U3A.Database;
using U3A.Model;

namespace U3A.WebFunctions.Procedures;

public static class DatabaseCleanup
{
    public static async Task Process(TenantInfo tenant, string connectionString, ILogger logger)
    {
        using (var dbc = new U3ADbContext(tenant))
        {
            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
            try
            {
                _ = await dbc.Database.ExecuteSqlRawAsync(@"execute [dbo].[prcDbCleanup]");
                logger.LogInformation($"[{tenant.Identifier}]: Execute [dbo].[prcDbCleanup] completed.");
            }
            catch (Exception ex)
            {
                logger.LogError("[{tenant.Identifier}]: Execute [dbo].[prcDbCleanup] failed: " + ex.Message);
            }
        }
        using (var dbc = new TenantDbContext(connectionString))
        {
            var cutOff = DateTime.UtcNow.Date.AddDays(-3);
            var tableName = "LogAutoEnrol";
            await deleteLogs(tenant, logger, dbc, cutOff, tableName);
            tableName = "LogEvents";
            await deleteLogs(tenant, logger, dbc, cutOff, tableName);
        }
    }
    static async Task deleteLogs(TenantInfo tenant, ILogger logger, TenantDbContext dbc, DateTime cutOff, string tableName)
    {
        var total = 0;
        try
        {
            while (true)
            {
                var cmd = $"delete top (5000) from [dbo].[{tableName}] where timestamp < '{cutOff.ToString("dd-MMM-yyyy")}'";
                var count = await dbc.Database.ExecuteSqlRawAsync(cmd);
                if (count <= 0) { break; }
                total += count;
                logger.LogInformation($"[{tenant.Identifier}]: {total} records deleted from [dbo].[{tableName}]");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"[{tenant.Identifier}]: Delete from [dbo].[{tableName}] failed: {ex.Message}");
        }
    }
}

