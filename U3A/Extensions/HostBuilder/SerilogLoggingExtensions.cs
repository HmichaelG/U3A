using Serilog.Core;
using Serilog.Events;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.SystemConsole.Themes;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using Serilog.Exceptions;
using Serilog.Filters;

namespace U3A.Extensions.HostBuilder;

public static class SerilogLoggingExtensions
{
    public static WebApplicationBuilder UseSerilogLogging(this WebApplicationBuilder builder, string TenantConnectionString)
    {
        LoggingLevelSwitch levelSwitch
            = new LoggingLevelSwitch(LogEventLevel.Warning);

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
            .MinimumLevel.Override("Microsoft.AspNetCore", levelSwitch)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
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
