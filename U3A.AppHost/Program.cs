var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.U3A>("u3a");

builder.AddProject<Projects.U3A_Super>("u3a-super");

builder.AddProject<Projects.U3A_WebFunctions>("u3a-webfunctions");

builder.Build().Run();
