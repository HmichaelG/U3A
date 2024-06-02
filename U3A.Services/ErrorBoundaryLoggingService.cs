using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

namespace U3A.Services
{
    public class ErrorBoundaryLoggingService : IErrorBoundaryLogger
    {
        readonly IDbContextFactory<TenantDbContext> _tenantDbFactory;
        readonly TenantInfoService _tenantInfoSvc;
        readonly ILogger<ErrorBoundaryLoggingService> _logger;
        public ErrorBoundaryLoggingService(IDbContextFactory<TenantDbContext> TenantDbFactory,
                                            ILogger<ErrorBoundaryLoggingService> logger,
                                            TenantInfoService TenantInfoService)
        {
            _tenantInfoSvc = TenantInfoService;
            _tenantDbFactory = TenantDbFactory;
        }
        public async ValueTask LogErrorAsync(Exception exception)
        {
            using (var dbc = await _tenantDbFactory.CreateDbContextAsync())
            {
                _logger.LogError(exception.ToString());
                var ex = new ExceptionLog() { Tenant = await _tenantInfoSvc.GetTenantIdentifierAsync(), 
                                                Log = exception.ToString() };
                await dbc.AddAsync(ex);
                var expiredLogs = dbc.ExceptionLog.Where(x => x.Date > DateTime.UtcNow.AddDays(30)).ToList();
                dbc.RemoveRange(expiredLogs);
                await dbc.SaveChangesAsync();
            }
            return;
        }
    }
}
