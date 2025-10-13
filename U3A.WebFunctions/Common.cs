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
            using (U3ADbContext dbc = new(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                SystemSettings? settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
                if (settings != null) { result = settings.DisableBackgroundProcessing; }
            }
            return result;
        }

        public static void GetTenants(List<TenantInfo> tenants, string ConnectionString, string tenant = "")
        {
            using SqlConnection cnn = new(ConnectionString);
            string whereClause = string.IsNullOrEmpty(tenant) ? "" : $"WHERE Identifier = '{tenant}'";
            string cmdText = @$"SELECT Identifier, 
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
            using SqlCommand cmd = new(cmdText, cnn);
            SqlDataReader? rdr;
            try
            {
                cnn.Open();
                rdr = cmd.ExecuteReader();
                if (rdr != null)
                {
                    while (rdr.Read())
                    {
                        TenantInfo t = new()
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
            catch (Exception)
            {
                EventId eID = new(10000);
            }
            finally
            {
                cnn.Close();
            }
        }

        public static TenantInfo? GetTenant(string tenantToProcess, string connectionString)
        {
            List<TenantInfo> tenants = [];
            Common.GetTenants(tenants, connectionString!, tenantToProcess);
            return (tenants.Count > 0) ? tenants.ToArray()[0] : null;
        }

        public static async Task<DateTime> GetTodayAsync(U3ADbContext dbc)
        {
            return (await GetNowAsync(dbc)).Date;
        }
        public static async Task<DateTime> GetNowAsync(U3ADbContext dbc)
        {
            // Get system settings
            TimeSpan utcOffset = await GetUtcOffsetAsync(dbc);
            return DateTime.UtcNow + utcOffset;
        }
        public static async Task<TimeSpan> GetUtcOffsetAsync(U3ADbContext dbc)
        {
            // Get system settings
            SystemSettings? settings = await dbc.SystemSettings
                                .OrderBy(x => x.ID)
                                .FirstOrDefaultAsync();
            return settings!.UTCOffset;
        }

    }
}
