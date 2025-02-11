using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Globalization;
using U3A.Model;
using Serilog.Sinks.SystemConsole.Themes;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog.Extensions.Logging;
using Microsoft.Extensions.Logging;


DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureLogging((hostingContext, logging) =>
    {
        var filePath = string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("WEBSITE_OS")) ?
                        "log.txt" :
                        @"D:\home\LogFiles\Application\log.txt";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .MinimumLevel.Override("Worker", LogEventLevel.Warning)
            .MinimumLevel.Override("Host", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Error)
            .MinimumLevel.Override("Function", LogEventLevel.Debug)
            .MinimumLevel.Override("Azure.Storage", LogEventLevel.Error)
            .MinimumLevel.Override("Azure.Core", LogEventLevel.Error)
            .MinimumLevel.Override("Azure.Identity", LogEventLevel.Error)
            .Enrich.WithProperty("Application", "SHDev Blog Functions")
            .Enrich.FromLogContext()
            .WriteTo.Console(LogEventLevel.Debug)
            .WriteTo.File(filePath, LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        logging.AddSerilog(Log.Logger, true);
    })
    .Build();

string var = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "";
if (var == "Development")
{
    constants.IS_DEVELOPMENT = true;
}

host.Run();

