using DevExpress.Drawing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;
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
//#if DEBUG
          //  , RunOnStartup=true
//#endif            
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
            Common.GetTenants(tenants, cn!);
            _logger.LogInformation($"{tenants.Count} tenants retrieved from database.");

            _logger.LogInformation("UTC Time: {0}", DateTime.UtcNow);

            bool isBackgroundProcessingEnabled = true;
            List<Task> TaskList = new List<Task>();
            TimeSpan utcOffset;
            foreach (var tenant in tenants)
            {
                _logger.LogInformation($"****** Processing Daily Procedures for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    using (var dbc = new U3ADbContext(tenant))
                    {
                        utcOffset = await Common.GetUtcOffsetAsync(dbc);
                        dbc.UtcOffset = utcOffset;
                        _logger.LogInformation($"[{tenant.Identifier}] Local Time: {DateTime.UtcNow + utcOffset}. UTC Offset: {utcOffset}");
                    }

                    isBackgroundProcessingEnabled = !await Common.isBackgroundProcessingDisabled(tenant);
                    RandomAllocationExecuted.Add(tenant.Identifier!, false);

                    _logger.LogInformation(">>>> Finalise outstanding online payments <<<");
                    try
                    {
                        await FinaliseOnlinePayment.Process(tenant, _logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing FinaliseOnlinePayment for {tenant.Identifier}");
                    }

                    _logger.LogInformation(">>>> Auto enroll participants <<<");
                    try
                    {
                        await AutoEnrollParticipants.Process(tenant, cn!, _logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing AutoEnrollParticipants for {tenant.Identifier}");
                    }

                    _logger.LogInformation(">>>> Bring forward enrolments <<<");
                    try
                    {
                        await BringForwardEnrolments.Process(tenant, _logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing BringForwardEnrolments for {tenant.Identifier}");
                    }

                    _logger.LogInformation(">>>> Today's attendance records <<<");
                    try
                    {
                        if (isBackgroundProcessingEnabled) TaskList.Add(CreateAttendance.Process(tenant, _logger));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing CreateAttendance for {tenant.Identifier}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing Daily Procedures for {tenant.Identifier}");
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
                        try
                        {
                            await ProcessCorrespondence.Process(tenant, cn!, _logger, IsHourlyProcedure: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing ProcessCorrespondence for {tenant.Identifier}");
                        }
                        try
                        {
                            await SendLeaderReports.Process(tenant, _logger);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing SendLeaderReports for {tenant.Identifier}");
                        }
                        try
                        {
                            await ProcessMembershipCoordinatorEmail.Process(tenant, _logger);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing ProcessMembershipCoordinatorEmail for {tenant.Identifier}");
                        }
                        try
                        {
                            await ProcessQueuedDocuments.Process(tenant, _logger);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing ProcessQueuedDocuments for {tenant.Identifier}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing Email for {tenant.Identifier}");
                }
            }

            _logger.LogInformation($"****** Create Class Schedule Cache ******");
            foreach (var tenant in tenants)
            {
                try
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing Class Schedule cache for {tenant.Identifier}");
                }
            }

            _logger.LogInformation($"****** Database Cleanup ******");
            foreach (var tenant in tenants)
            {
                try
                {
                    await DatabaseCleanup.Process(tenant, _logger);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing DatabaseCleanup for {tenant.Identifier}");
                }
            }

            _logger.LogInformation($"Daily Procedures completed at: {DateTime.UtcNow}");
        }
    }

}
