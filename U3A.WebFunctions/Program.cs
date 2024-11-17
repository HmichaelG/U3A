using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using static DevExpress.Data.Utils.AnnotationAttributes;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Data;
using U3A.WebFunctions;
using Microsoft.Extensions.Configuration;
using DevExpress.Drawing;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;


DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);

var builder = FunctionsApplication.CreateBuilder(args);

string? tenantConnectionString = builder.Configuration.GetConnectionString(Common.TENANT_CN_CONFIG);

DevExpress.Drawing.Internal.DXDrawingEngine.ForceSkia();
foreach (var file in Directory.GetFiles(@"fonts"))
{
    DXFontRepository.Instance.AddFont(file);
}


builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
