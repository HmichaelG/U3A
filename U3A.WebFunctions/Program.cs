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
            .Enrich.FromLogContext()
            .WriteTo.OpenTelemetry()
            //.WriteTo.Console(LogEventLevel.Information)
            .WriteTo.File(filePath, 
                        LogEventLevel.Information, 
                        shared: true,
                        rollingInterval: RollingInterval.Day)
            .CreateLogger();
        logging.AddSerilog(Log.Logger,true);
        constants.IS_DEVELOPMENT = hostingContext.HostingEnvironment.IsDevelopment();
    })
    .Build();

host.Run();

