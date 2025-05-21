var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DreamTravel_Api>("dreamtravel-api");

builder.AddProject<Projects.DreamTravel_Worker>("dreamtravel-worker");

builder.AddProject<Projects.DreamTravel_Ui>("dreamtravel-ui");

builder.Build().Run();
