using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var mainApp = builder.AddProject<Projects.U3A>("u3a");

var api = builder.AddAzureFunctionsProject<Projects.U3A_WebFunctions>("u3a-webfunctions");

mainApp.WithEnvironment("aspire-webfunctions-url", api.GetEndpoint("http"));

builder.Build().Run();
