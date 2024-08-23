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
    public class HourlyProcedures
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public HourlyProcedures(ILoggerFactory loggerFactory,
                            IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<HourlyProcedures>();
            _config = config;
        }

        [Function("HouryProcedures")]
        public async Task Run([TimerTrigger("0 0 22-23,0-11 * * *"      
#if DEBUG
            //, RunOnStartup=true
#endif            
            )] TimerInfo myTimer)
        {

            // Get the fonts
            foreach (var file in System.IO.Directory.GetFiles(@"fonts"))
            {
                DXFontRepository.Instance.AddFont(file);
            }

            //Retrieve the tenants
            var tenants = new List<TenantInfo>();
            var cn = _config.GetConnectionString(Common.TENANT_CN_CONFIG);
            Common.GetTeanats(tenants, cn!);

            _logger.LogInformation($"{tenants.Count} tenants retrieved from database.");
            _logger.LogInformation($"UTC Time is: {DateTime.UtcNow}");

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
                TaskList.Add(AutoEnrolParticipants.Process(tenant, cn!, _logger));
            }
            // Make sure all processing is complete before email starts.
            Task.WaitAll(TaskList.ToArray());

            foreach (var tenant in tenants)
            {
                _logger.LogInformation($"****** Processing Email Procedures for {tenant.Identifier}: {tenant.Name}. ******");
                isBackgroundProcessingEnabled = !await Common.isBackgroundProcessingDisabled(tenant);
                if (isBackgroundProcessingEnabled)
                {
                    await ProcessCorrespondence.Process(tenant, cn!, _logger, IsHourlyProcedure: true);
                }
                else
                {
                    _logger.LogInformation($"[{tenant.Identifier}]: Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                }
            }
            foreach (var tenant in tenants)
            {
                using (var dbc = new U3ADbContext(tenant))
                {
                    dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                    var hour = (DateTime.Now + dbc.UtcOffset).Hour;
                    List<int> execHours = new() { 8, 12, 18 };
                    if (execHours.Contains(hour))
                    {
                        using (var dbcT = new TenantDbContext(cn!))
                        {
                            await BusinessRule.BuildScheduleAsync(dbc, dbcT, tenant.Identifier!);
                        }
                        _logger.LogInformation($"Class Schedule cache created for: {tenant.Identifier}.");
                    }
                }
            }
            _logger.LogInformation($"Hourly Procedures complete at: {DateTime.UtcNow} UTC");
        }

    }

}
