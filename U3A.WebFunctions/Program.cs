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


DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);

var builder = WebApplication.CreateBuilder(args);
string? tenantConnectionString = builder.Configuration.GetConnectionString(Common.TENANT_CN_CONFIG);

var columnOptions = new SeriSQL.ColumnOptions
{
    AdditionalColumns = new Collection<SeriSQL.SqlColumn>
    {
        new SeriSQL.SqlColumn
                { ColumnName = "Tenant", PropertyName = "Tenant", DataType = SqlDbType.NVarChar, DataLength = 64 },
        new SeriSQL.SqlColumn
                { ColumnName = "User", PropertyName = "User", DataType = SqlDbType.NVarChar, DataLength = 64 },
        new SeriSQL.SqlColumn
                { ColumnName = "LogEvent", PropertyName = "LogEvent", DataType = SqlDbType.NVarChar, DataLength = 64 },
    }
};

DevExpress.Drawing.Internal.DXDrawingEngine.ForceSkia();
foreach (var file in Directory.GetFiles(@"fonts"))
{
    DXFontRepository.Instance.AddFont(file);
}


var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
     .ConfigureLogging((hostingContext, logging) =>
     {
         Log.Logger = new LoggerConfiguration()
             .Enrich.FromLogContext()
             .Enrich.WithExceptionDetails()
             .WriteTo.Logger(lc => lc
                 .Filter.ByIncludingOnly(Matching.WithProperty("AutoEnrolParticipants"))
                 .WriteTo.MSSqlServer(connectionString: tenantConnectionString,
                                         formatProvider: new CultureInfo("en-AU"),                                         
                                         sinkOptions: new SeriSQL.MSSqlServerSinkOptions
                                         {
                                             TableName = "LogAutoEnrol"
                                         },
                                             columnOptions: columnOptions
                                         )
                 )
             .CreateLogger();

         logging.AddSerilog(Log.Logger, true);
     })
    .Build();
host.Run();

