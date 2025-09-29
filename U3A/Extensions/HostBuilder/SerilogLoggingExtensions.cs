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
using U3A.Model;

namespace U3A.Extensions.HostBuilder;

public static class SerilogLoggingExtensions
{
    public static WebApplicationBuilder UseSerilogLogging(this WebApplicationBuilder builder, string TenantConnectionString)
    {
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
            .Enrich.WithExceptionDetails()
            .Filter.ByExcluding(logEvent =>
                logEvent.Exception is OperationCanceledException ||
                logEvent.Exception is ObjectDisposedException ||
                logEvent.Exception is AntiforgeryValidationException ||
                logEvent.Exception is CryptographicException ||
                logEvent.Exception is JSDisconnectedException
                )
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.WithProperty("AutoEnrolParticipants"))
                .WriteTo.MSSqlServer(connectionString: TenantConnectionString,
                                        formatProvider: new CultureInfo("en-AU"),
                                        sinkOptions: new MSSqlServerSinkOptions
                                        {
                                            TableName = "LogAutoEnrol"
                                        },
                                        columnOptions: columnOptions
                                    )
                )
            .WriteTo.Console(formatProvider: new CultureInfo("en-AU"),
                        theme: AnsiConsoleTheme.Sixteen,
                        applyThemeToRedirectedOutput: true)
            .WriteTo.OpenTelemetry()
            .WriteTo.MSSqlServer(connectionString: TenantConnectionString,
                                    formatProvider: new CultureInfo("en-AU"),
                                    restrictedToMinimumLevel: LogEventLevel.Error,
                                    sinkOptions: new MSSqlServerSinkOptions
                                    {
                                        TableName = "LogEvents"
                                    },
                                    columnOptions: columnOptions
                                )
            .CreateLogger();

        builder.Host.UseSerilog(Log.Logger);
        Log.Information("Logging started {now}", DateTime.Now);

        return builder;
    }
}