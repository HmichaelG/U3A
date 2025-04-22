using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Globalization;
using U3A.Model;


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
            .MinimumLevel.Override("Azure", LogEventLevel.Error)
            .MinimumLevel.Override("Aspire", LogEventLevel.Error)
            .Enrich.FromLogContext()
            .WriteTo.OpenTelemetry()
            .WriteTo.Console(formatProvider: new CultureInfo("en-AU"),
                        theme: AnsiConsoleTheme.Sixteen,
                        applyThemeToRedirectedOutput: true)
            .WriteTo.File(filePath,
                        LogEventLevel.Information,
                        shared: true,
                        rollingInterval: RollingInterval.Day)
            .CreateLogger();
        logging.AddSerilog(Log.Logger, true);
        constants.IS_DEVELOPMENT = hostingContext.HostingEnvironment.IsDevelopment();
    })
    .Build();

host.Run();

