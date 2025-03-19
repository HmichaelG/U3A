using Aspire.Hosting;
using Serilog;
using Serilog.Events;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddSerilog(config =>
{
    config.WriteTo.Console();
});

Log.Information("Aspire Host has started");

var mainApp = builder.AddProject<Projects.U3A>("u3a");

var api = builder.AddAzureFunctionsProject<Projects.U3A_WebFunctions>("u3a-webfunctions");

mainApp.WithEnvironment("aspire-webfunctions-url", api.GetEndpoint("http"));

builder.Build().Run();
