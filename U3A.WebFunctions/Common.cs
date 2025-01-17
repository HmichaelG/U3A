using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using U3A.Database;
using U3A.Model;


namespace U3A.WebFunctions
{
    public static class Common
    {

        public const string TENANT_CN_CONFIG = "TenantConnectionString";
        public static async Task<bool> isBackgroundProcessingDisabled(TenantInfo tenant)
        {
            bool result = false;
            using (var dbc = new U3ADbContext(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
                if (settings != null) { result = settings.DisableBackgroundProcessing; }
            }
            return result;
        }

        public static void GetTenants(List<TenantInfo> tenants, string ConnectionString, string tenant = "")
        {
            using (var cnn = new SqlConnection(ConnectionString))
            {
                var whereClause = (string.IsNullOrEmpty(tenant) ? "" : $"WHERE Identifier = '{tenant}'");
                var cmdText = @$"SELECT Identifier, 
                                        Name, 
                                        ConnectionString,
                                        EwayAPIKey,
                                        EwayPassword,
                                        UseEwayTestEnviroment,
                                        PostmarkAPIKey, 
                                        PostmarkSandboxAPIKey, 
                                        UsePostmarkTestEnviroment 
                                        FROM TenantInfo 
                                        {whereClause}
                                        ORDER BY Identifier";
                using (var cmd = new SqlCommand(cmdText, cnn))
                {
                    SqlDataReader? rdr;
                    try
                    {
                        cnn.Open();
                        rdr = cmd.ExecuteReader();
                        if (rdr != null)
                        {
                            while (rdr.Read())
                            {
                                var t = new TenantInfo()
                                {
                                    Identifier = rdr[0].ToString(),
                                    Name = rdr[1].ToString(),
                                    ConnectionString = rdr[2].ToString(),
                                    EwayAPIKey = rdr[3].ToString(),
                                    EwayPassword = rdr[4].ToString(),
                                    UseEwayTestEnviroment = rdr.GetBoolean(5),
                                    PostmarkAPIKey = rdr[6].ToString(),
                                    PostmarkSandboxAPIKey = rdr[7].ToString(),
                                    UsePostmarkTestEnviroment = rdr.GetBoolean(8)
                                };
                                tenants.Add(t);
                            }
                            rdr.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        var eID = new EventId(10000);
                    }
                    finally
                    {
                        cnn.Close();
                    }
                }
            }
        }

        public static async Task<DateTime> GetTodayAsync(U3ADbContext dbc)
        {
            return (await GetNowAsync(dbc)).Date;
        }
        public static async Task<DateTime> GetNowAsync(U3ADbContext dbc)
        {
            // Get system settings
            var utcOffset = await GetUtcOffsetAsync(dbc);
            return DateTime.UtcNow + utcOffset;
        }
        public static async Task<TimeSpan> GetUtcOffsetAsync(U3ADbContext dbc)
        {
            // Get system settings
            var settings = await dbc.SystemSettings
                                .OrderBy(x => x.ID)
                                .FirstOrDefaultAsync();
            return settings!.UTCOffset;
        }

    }
}
