using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace U3A.Services;
public class SerilogEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SerilogEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Get HttpContext properties here
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // Add properties to the log event based on HttpContext
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("Tenant", httpContext.Request.Host));
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("User", httpContext.User.Identity.Name));
        }
    }
}
