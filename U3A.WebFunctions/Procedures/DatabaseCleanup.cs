using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using U3A.Database;
using U3A.Model;

namespace U3A.WebFunctions.Procedures
{
    public static class DatabaseCleanup
    {
        public static async Task Process(TenantInfo tenant, ILogger logger)
        {
            using (var dbc = new U3ADbContext(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                try
                {
                    _ = await dbc.Database.ExecuteSqlRawAsync(@"execute [dbo].[prcDbCleanup]");
                    logger.LogInformation("Execute [dbo].[prcDbCleanup] completed.");
                }
                catch (Exception ex)
                {
                    logger.LogError("Execute [dbo].[prcDbCleanup] failed: " + ex.Message);
                }
            }
        }
    }
}