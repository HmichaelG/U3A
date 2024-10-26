using DevExpress.Drawing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using U3A.BusinessRules;
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

            var tenants = new List<TenantInfo>();
            var cn = _config.GetConnectionString(Common.TENANT_CN_CONFIG);
            Common.GetTeanats(tenants, cn!);
            _logger.LogInformation($"{tenants.Count} tenants retrieved from database.");

            _logger.LogInformation("UTC Time: {0}", DateTime.UtcNow);

            bool isBackgroundProcessingEnabled = true;
            List<Task> TaskList = new List<Task>();
            TimeSpan utcOffset;
            foreach (var tenant in tenants)
            {
                using (var dbc = new U3ADbContext(tenant))
                {
                    utcOffset = await Common.GetUtcOffsetAsync(dbc);
                    dbc.UtcOffset = utcOffset;
                    _logger.LogInformation($"[{tenant.Identifier}] Local Time: {DateTime.UtcNow + utcOffset}. UTC Offset: {utcOffset}");
                }

                isBackgroundProcessingEnabled = !await Common.isBackgroundProcessingDisabled(tenant);
                _logger.LogInformation($"****** Processing Daily Procedures for {tenant.Identifier}: {tenant.Name}. ******");
                RandomAllocationExecuted.Add(tenant.Identifier!, false);
                TaskList.Add(FinaliseOnlinePayment.Process(tenant, _logger));
                TaskList.Add(AutoEnrolParticipants.Process(tenant, cn!, _logger));
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
                isBackgroundProcessingEnabled = !await Common.isBackgroundProcessingDisabled(tenant);
                if (isBackgroundProcessingEnabled)
                {

                    await ProcessCorrespondence.Process(tenant, cn!, _logger, IsHourlyProcedure: false);
                    await SendLeaderReports.Process(tenant, _logger);
                    await ProcessMembershipCoordinatorEmail.Process(tenant, _logger);
                    await ProcessQueuedDocuments.Process(tenant, _logger);

                }
                else
                {
                    _logger.LogInformation($"Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                }
            }

            foreach (var tenant in tenants)
            {
                using (var dbc = new U3ADbContext(tenant))
                {
                    utcOffset = await Common.GetUtcOffsetAsync(dbc);
                    dbc.UtcOffset = utcOffset;
                    using (var dbcT = new TenantDbContext(cn!))
                    {
                        await BusinessRule.BuildScheduleAsync(dbc, dbcT, tenant.Identifier!);
                    }
                    _logger.LogInformation($"Class Schedule cache created for: {tenant.Identifier}.");
                }
            }

            foreach (var tenant in tenants)
            {
                await DatabaseCleanup.Process(tenant, _logger);
            }

            _logger.LogInformation($"Daily Procedures completed at: {DateTime.UtcNow}");
        }
    }

}
