var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SolTechnology_TaleCode_Api>("soltechnology-talecode-api");

builder.AddProject<Projects.SolTechnology_TaleCode_Worker>("soltechnology-talecode-worker");

builder.Build().Run();
