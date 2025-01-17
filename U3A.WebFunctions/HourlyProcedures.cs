using DevExpress.Drawing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
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

        [Function("HourlyProcedures")]
        public async Task Run([TimerTrigger("0 0 22-23,0-11 * * *"      
#if DEBUG
           // , RunOnStartup=true
#endif            
            )] TimerInfo myTimer)
        {
            await DoWork();
        }

        private async Task DoWork() {

            //Retrieve the tenants
            var tenants = new List<TenantInfo>();
            var cn = _config.GetConnectionString(Common.TENANT_CN_CONFIG);
            Common.GetTenants(tenants, cn!);

            _logger.LogInformation($"{tenants.Count} tenants retrieved from database.");
            _logger.LogInformation($"UTC Time is: {DateTime.UtcNow}");

            bool isBackgroundProcessingEnabled = true;
            List<Task> TaskList = new List<Task>();

            TimeSpan utcOffset;
            foreach (var tenant in tenants)
            {
                _logger.LogInformation($"****** Processing AutoEnrollParticipants for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    using (var dbc = new U3ADbContext(tenant))
                    {
                        utcOffset = await Common.GetUtcOffsetAsync(dbc);
                        dbc.UtcOffset = utcOffset;
                        _logger.LogInformation($"[{tenant.Identifier}] Local Time: {DateTime.UtcNow + utcOffset}. UTC Offset: {utcOffset}");
                    }
                    await AutoEnrollParticipants.Process(tenant, cn!, _logger);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing AutoEnrollParticipants for {tenant.Identifier}");
                }
            }

            foreach (var tenant in tenants)
            {
                _logger.LogInformation($"****** Processing Email for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing Email for {tenant.Identifier}");
                }
            }

            foreach (var tenant in tenants)
            {
                try
                {
                    using (var dbc = new U3ADbContext(tenant))
                    {
                        dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                        using (var dbcT = new TenantDbContext(cn!))
                        {
                            await BusinessRule.BuildScheduleAsync(dbc, dbcT, tenant.Identifier!);
                        }
                        _logger.LogInformation($"Class Schedule cache created for: {tenant.Identifier}.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing Class Schedule cache for {tenant.Identifier}");
                }
            }
            _logger.LogInformation($"Hourly Procedures complete at: {DateTime.UtcNow} UTC");
        }

    }

}
