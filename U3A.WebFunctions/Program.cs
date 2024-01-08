using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();
host.Run();