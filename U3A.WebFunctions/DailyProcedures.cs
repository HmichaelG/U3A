using DevExpress.Drawing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using U3A.Database;
using U3A.Model;
using U3A.WebFunctions.Procedures;

namespace U3A.WebFunctions
{
    public class DailyProcedures
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        public static Dictionary<string, bool> RandomAllocationExecuted = new();

        public DailyProcedures(ILoggerFactory loggerFactory,
                            IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<DailyProcedures>();
            _config = config;
        }

        [Function("DailyProcedures")]
        public async Task Run([TimerTrigger("0 0 17 * * *"      
#if DEBUG
            , RunOnStartup=true
#endif            
            )] TimerInfo myTimer)
        {

            // Get the fonts
            foreach (var file in System.IO.Directory.GetFiles(@"fonts"))
            {
                DXFontRepository.Instance.AddFont(file);
            }

            RandomAllocationExecuted = new Dictionary<string, bool>();
            //Retrieve the tenants
            _logger.LogInformation($"Retrieve tenant list from database.");
            var tenants = new List<TenantInfo>();
            var cn = _config.GetConnectionString(Common.TENANT_CN_CONFIG);
            Common.GetTeanats(tenants, cn!);
            _logger.LogInformation($"{tenants.Count} tenants retrieved from database.");

            _logger.LogInformation("UTC Time: {0}", DateTime.UtcNow);

            bool isBackgroundProcessingEnabled = true;
            List<Task> TaskList = new List<Task>();
            foreach (var tenant in tenants)
            {
                isBackgroundProcessingEnabled = !(await Common.isBackgroundProcessingDisabled(tenant));
                _logger.LogInformation($"****** Processing Daily Procedures for {tenant.Identifier}: {tenant.Name}. ******");
                using (var dbc = new U3ADbContext(tenant))
                {
                    _logger.LogInformation("Local Time for {0} is: {1}", tenant.Identifier, await Common.GetNowAsync(dbc));
                }
                RandomAllocationExecuted.Add(tenant.Identifier, false);
                TaskList.Add(FinaliseOnlinePayment.Process(tenant, _logger));
                TaskList.Add(AutoEnrolParticipants.Process(tenant, _logger));
                await BringForwardEnrolments.Process(tenant, _logger);
                if (isBackgroundProcessingEnabled) TaskList.Add(CreateAttendance.Process(tenant, _logger));
            }
            // Make sure all processing is complete before email starts.
            Task.WaitAll(TaskList.ToArray());
            // Make sure we are email at a later time then we process.
            Thread.Sleep(3000);

            foreach (var tenant in tenants)
            {
                _logger.LogInformation($"****** Processing Email Procedures for {tenant.Identifier}: {tenant.Name}. ******");
                isBackgroundProcessingEnabled = !(await Common.isBackgroundProcessingDisabled(tenant));
                if (isBackgroundProcessingEnabled)
                {
                    await ProcessCorrespondence.Process(tenant, _logger, IsHourlyProcedure: false);
                    await SendLeaderReports.Process(tenant, _logger);
#if !DEBUG
                    await ProcessMembershipCoordinatorEmail.Process(tenant, _logger);
#endif
                    await ProcessQueuedDocuments.Process(tenant, _logger);
                }
                else
                {
                    _logger.LogInformation($"Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                }
            }
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {myTimer!.ScheduleStatus!.Next}");
        }

        //async Task<bool> isBackgroundProcessingDisabled(TenantInfo tenant)
        //{
        //    bool result = false;
        //    using (var dbc = new U3ADbContext(tenant))
        //    {
        //        var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
        //        if (settings != null) { result = settings.DisableBackgroundProcessing; }
        //    }
        //    return result;
        //}

        //private void GetTeanats(List<TenantInfo> tenants)
        //{
        //    var cn = _config.GetConnectionString("TenantConnectionString");
        //    using (var cnn = new SqlConnection(cn))
        //    {
        //        var cmdText = @"SELECT Identifier, 
        //                                Name, 
        //                                ConnectionString,
        //                                EwayAPIKey,
        //                                EwayPassword,
        //                                UseEwayTestEnviroment,
        //                                PostmarkAPIKey, 
        //                                PostmarkSandboxAPIKey, 
        //                                UsePostmarkTestEnviroment FROM TenantInfo";
        //        using (var cmd = new SqlCommand(cmdText, cnn))
        //        {
        //            SqlDataReader? rdr;
        //            try
        //            {
        //                cnn.Open();
        //                rdr = cmd.ExecuteReader();
        //                if (rdr != null)
        //                {
        //                    while (rdr.Read())
        //                    {
        //                        var t = new TenantInfo()
        //                        {
        //                            Identifier = rdr[0].ToString(),
        //                            Name = rdr[1].ToString(),
        //                            ConnectionString = rdr[2].ToString(),
        //                            EwayAPIKey = rdr[3].ToString(),
        //                            EwayPassword = rdr[4].ToString(),
        //                            UseEwayTestEnviroment = rdr.GetBoolean(5),
        //                            PostmarkAPIKey = rdr[6].ToString(),
        //                            PostmarkSandboxAPIKey = rdr[7].ToString(),
        //                            UsePostmarkTestEnviroment = rdr.GetBoolean(8)
        //                        };
        //                        tenants.Add(t);
        //                    }
        //                    rdr.Close();
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                var eID = new EventId(10000);
        //                _logger.LogError(eID, ex.Message, "Error retrieving TenantInfo");
        //            }
        //            finally
        //            {
        //                cnn.Close();
        //            }
        //        }
        //    }
        //}

        //public static async Task<DateTime> GetTodayAsync(U3ADbContext dbc)
        //{
        //    return (await GetNowAsync(dbc)).Date;
        //}
        //public static async Task<DateTime> GetNowAsync(U3ADbContext dbc)
        //{
        //    // Get system settings
        //    var settings = await dbc.SystemSettings
        //                        .OrderBy(x => x.ID)
        //                        .FirstOrDefaultAsync();
        //    TimezoneAdjustment.TimezoneOffset = new TimeSpan(settings.UTCOffset, 0, 0);
        //    return DateTime.UtcNow.AddHours(settings.UTCOffset);
        //}

    }

}
