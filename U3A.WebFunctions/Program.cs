using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using U3A.Database;
using System.Configuration;
using DevExpress.XtraSpellChecker;

DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();
host.Run();

