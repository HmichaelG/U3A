using Aspire.Hosting;
using Serilog;
using Serilog.Events;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddAzureFunctionsProject<Projects.U3A_WebFunctions>("u3a-webfunctions");

var mainApp = builder.AddProject<Projects.U3A>("u3a").WaitFor(api);

mainApp.WithEnvironment("aspire-webfunctions-url", api.GetEndpoint("http"));

builder.Build().Run();
