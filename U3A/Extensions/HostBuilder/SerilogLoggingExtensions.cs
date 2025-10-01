using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.JSInterop;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.SystemConsole.Themes;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using U3A.Services;

namespace U3A.Extensions.HostBuilder;

public static class SerilogLoggingExtensions
{
    /*
    Pseudocode / Plan:
    1. Ensure IHttpContextAccessor is registered in DI so SerilogEnricher can be activated.
    2. Register the HttpContext accessor with builder.Services.AddHttpContextAccessor().
    3. Register SerilogEnricher as a singleton.
    4. Build a temporary service provider (disposed after use) to resolve TelemetryConfiguration (if present) and SerilogEnricher.
    5. Configure Serilog using the resolved enricher and telemetry configuration.
    6. Attach Serilog to the host and log startup information.
    */
    public static WebApplicationBuilder UseSerilogLogging(this WebApplicationBuilder builder, string TenantConnectionString)
    {

        // Register the enricher so it can be resolved from the temporary provider
        builder.Services.AddSingleton<SerilogEnricher>();

        // Build a temporary service provider to resolve TelemetryConfiguration if it's registered.
        // If not available, fall back to a default TelemetryConfiguration.
        using var tempServiceProvider = builder.Services.BuildServiceProvider();
        var telemetryConfig = tempServiceProvider.GetService<TelemetryConfiguration>() ?? TelemetryConfiguration.CreateDefault();

        var enricher = tempServiceProvider.GetRequiredService<SerilogEnricher>();

        var columnOptions = new ColumnOptions
        {
            AdditionalColumns = new Collection<SqlColumn>
            {
                new SqlColumn
                { ColumnName = "Tenant", PropertyName = "Tenant", DataType = SqlDbType.NVarChar, DataLength = 64 },
                new SqlColumn
                { ColumnName = "User", PropertyName = "User", DataType = SqlDbType.NVarChar, DataLength = 64 },
                new SqlColumn
                { ColumnName = "LogEvent", PropertyName = "LogEvent", DataType = SqlDbType.NVarChar, DataLength = 64 },
            }
        };

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override(nameof(Microsoft.EntityFrameworkCore), LogEventLevel.Warning)
            .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Error)
            // Ensure enrichers run before the filter so properties are present
            .Enrich.FromLogContext()
            .Enrich.With(enricher)
            .Enrich.WithExceptionDetails()
            .Filter.ByExcluding(logEvent =>
                logEvent.Exception is OperationCanceledException ||
                logEvent.Exception is ObjectDisposedException ||
                logEvent.Exception is AntiforgeryValidationException ||
                logEvent.Exception is CryptographicException ||
                logEvent.Exception is JSDisconnectedException
                )
            .WriteTo.Async(a => a.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.WithProperty("AutoEnrolParticipants"))
                .WriteTo.Async(a => a.MSSqlServer(connectionString: TenantConnectionString,
                                        formatProvider: new CultureInfo("en-AU"),
                                        sinkOptions: new MSSqlServerSinkOptions
                                        {
                                            TableName = "LogAutoEnrol"
                                        },
                                        columnOptions: columnOptions
                                    )
                )))
            .WriteTo.Async(a => a.Console(formatProvider: new CultureInfo("en-AU"),
                        theme: AnsiConsoleTheme.Sixteen,
                        applyThemeToRedirectedOutput: true))
            .WriteTo.Async(a => a.OpenTelemetry())
            .WriteTo.Async(a => a.ApplicationInsights(
                        telemetryConfig,
                        TelemetryConverter.Traces))
            .WriteTo.Async(a => a.MSSqlServer(connectionString: TenantConnectionString,
                                    formatProvider: new CultureInfo("en-AU"),
                                    restrictedToMinimumLevel: LogEventLevel.Error,
                                    sinkOptions: new MSSqlServerSinkOptions
                                    {
                                        TableName = "LogEvents"
                                    },
                                    columnOptions: columnOptions
                                ))
            .CreateLogger();

        builder.Host.UseSerilog(Log.Logger);
        Log.Information("Logging started {now}", DateTime.Now);

        return builder;
    }
}