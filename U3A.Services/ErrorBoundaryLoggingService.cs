using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
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
        public async ValueTask LogErrorAsync(Exception exception)
        {
            var cnn = constants.TENANT_CONNECTION_STRING;
            using (var dbc = new TenantDbContext(cnn))
            {
                var ex = new ExceptionLog() { Tenant = constants.TENANT, Log = exception.ToString() };
                await dbc.AddAsync(ex);
                var expiredLogs = dbc.ExceptionLog.Where(x => x.Date > DateTime.UtcNow.AddDays(7)).ToList();
                dbc.RemoveRange(expiredLogs);
                await dbc.SaveChangesAsync();
            }
            return;
        }
    }
}
