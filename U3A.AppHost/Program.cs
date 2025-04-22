var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai")
    : builder.AddConnectionString("openai");

var api = builder.AddAzureFunctionsProject<Projects.U3A_WebFunctions>("u3a-webfunctions");

var mainApp = builder.AddProject<Projects.U3A>("u3a")
    .WithReference(openai)
    .WaitFor(api);

mainApp.WithEnvironment("aspire-webfunctions-url", api.GetEndpoint("http"));

builder.AddProject<Projects.U3A_Super>("u3a-super");

builder.Build().Run();
