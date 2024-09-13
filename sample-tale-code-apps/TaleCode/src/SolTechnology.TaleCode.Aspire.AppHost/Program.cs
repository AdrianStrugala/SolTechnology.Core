var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.SolTechnology_TaleCode_Aspire_ApiService>("apiservice");
// var api = builder.AddProject<Projects.SolTechnology_TaleCode_Aspire_ApiService>("apiservice");

builder.AddProject<Projects.SolTechnology_TaleCode_Aspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.AddProject<Projects.SolTechnology_TaleCode_Api>("soltechnology-talecode-api");

builder.AddProject<Projects.SolTechnology_TaleCode_Worker>("soltechnology-talecode-worker");

builder.Build().Run();
