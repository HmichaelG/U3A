using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace U3A.Services;

/// <summary>
/// Simplified error boundary logger using concise null checks and 'using var' for LogContext pushes.
/// </summary>
public class ErrorBoundaryLoggingService : IErrorBoundaryLogger
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public ErrorBoundaryLoggingService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /*
    Pseudocode / Plan:
    1. Obtain HttpContext from the IHttpContextAccessor.
    2. If HttpContext is null -> return immediately (nothing to log).
    3. Extract tenant from HttpContext.Request?.Host.Host.
       - If tenant is null or whitespace -> return (no tenant context).
    4. Extract user name from HttpContext.User?.Identity?.Name (fall back to empty string).
    5. Use 'using var' to push Serilog context properties:
       - "LogEvent" = "Unhandled Exception"
       - "Tenant" = tenant
       - "User" = user
       These are disposed automatically at method end.
    6. Log the exception with Log.Error, including the exception and its message.
    7. Return a completed ValueTask (no async/await needed).
    */

    public ValueTask LogErrorAsync(Exception exception)
    {
        var ctx = httpContextAccessor?.HttpContext;
        if (ctx == null)
            return ValueTask.CompletedTask;

        var tenant = ctx.Request?.Host.Host;
        if (string.IsNullOrWhiteSpace(tenant))
            return ValueTask.CompletedTask;

        var user = ctx.User?.Identity?.Name ?? string.Empty;

        using var _ev = LogContext.PushProperty("LogEvent", "Unhandled Exception");
        using var _t = LogContext.PushProperty("Tenant", tenant);
        using var _u = LogContext.PushProperty("User", user);
        Log.Error(exception, "{Message}", exception?.Message);
        return ValueTask.CompletedTask;
    }
}

