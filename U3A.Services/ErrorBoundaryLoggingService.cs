﻿using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;
using Serilog;
using Serilog.Context;

namespace U3A.Services
{
    public class ErrorBoundaryLoggingService : IErrorBoundaryLogger
    {
        readonly TenantInfoService _tenantInfoSvc;
        public ErrorBoundaryLoggingService(TenantInfoService TenantInfoService)
        {
            _tenantInfoSvc = TenantInfoService;
        }
        public async ValueTask LogErrorAsync(Exception exception)
        {
            using (LogContext.PushProperty("LogEvent","Unhandled Exception"))
            {
                using (LogContext.PushProperty("Tenant",
                            await _tenantInfoSvc.GetTenantIdentifierAsync()))
                {
                    using (LogContext.PushProperty("User",
                            _tenantInfoSvc.GetUserIdentity()))
                    {
                        Log.Error(exception,"{p0}",exception.Message);
                    }
                }
            }
            return;
        }
    }
}
