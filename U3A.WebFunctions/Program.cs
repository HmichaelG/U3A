using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;
using Serilog.Formatting.Json;
using SeriSQL = Serilog.Sinks.MSSqlServer;
using static DevExpress.Data.Utils.AnnotationAttributes;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Data;
using U3A.WebFunctions;
using Microsoft.Extensions.Configuration;
using DevExpress.Drawing;
using U3A.Model;
using Microsoft.EntityFrameworkCore.Metadata.Internal;


DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .Build();

string var = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "";
if (var == "Development")
{
    constants.IS_DEVELOPMENT = true;
}

host.Run();

