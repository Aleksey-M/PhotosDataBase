var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Mediafiles_App>("mediafiles-app");

builder.Build().Run();
