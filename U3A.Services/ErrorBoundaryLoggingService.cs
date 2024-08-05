using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
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
        private readonly IHttpContextAccessor httpContextAccessor;

        public ErrorBoundaryLoggingService(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }
        public async ValueTask LogErrorAsync(Exception exception)
        {
            using (LogContext.PushProperty("LogEvent", "Unhandled Exception"))
            {
                using (LogContext.PushProperty("Tenant",
                            httpContextAccessor?.HttpContext?.Request?.Host.Host))
                {
                    using (LogContext.PushProperty("User",
                            httpContextAccessor?.HttpContext.User.Identity.Name))
                    {
                        Log.Error(exception, "{p0}", exception.Message);
                    }
                }
            }
            return;
        }
    }
}
