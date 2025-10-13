using DevExpress.Drawing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Globalization;
using U3A.Model;


DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);
DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.Model.Class).Assembly);
DevExpress.Drawing.Settings.DrawingEngine = DrawingEngine.Skia;
foreach (string file in Directory.GetFiles(@"fonts"))
{
    DXFontRepository.Instance.AddFont(file);
}

IHost host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureLogging((hostingContext, logging) =>
    {
        string filePath = string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("WEBSITE_OS")) ?
                        "log.txt" :
                        @"D:\home\LogFiles\Application\log.txt";

        Log.Logger = new LoggerConfiguration()
            // *** Comment to view SQL statements ***
            .MinimumLevel.Override(nameof(Microsoft.EntityFrameworkCore), LogEventLevel.Warning)
            .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Error)
            .MinimumLevel.Override(nameof(Azure.Storage), LogEventLevel.Error)
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
        _ = logging.AddSerilog(Log.Logger, true);
        constants.IS_DEVELOPMENT = hostingContext.HostingEnvironment.IsDevelopment();
    })
    .Build();

host.Run();

