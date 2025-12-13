using Microsoft.Data.SqlClient;
using SolTechnology.Core.Sql.SqlProject;

var builder = DistributedApplication.CreateBuilder(args);

var dbPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder
    .AddSqlServer("sql", dbPassword)
    .WithHostPort(1404)
    .WithLifetime(ContainerLifetime.Persistent);

var dreamTravelDb = sql.AddDatabase("DreamTravelDatabase");

var neo4j = builder.AddContainer("neo4j", "neo4j", "5.10-community")
    .WithEnvironment("NEO4J_AUTH", "neo4j/neo4jpass")
    .WithEnvironment("NEO4J_dbms_directories_import", "/var/lib/neo4j/import")
    .WithEnvironment("NEO4J_dbms_security_procedures_unrestricted", "apoc.*")
    .WithEnvironment("NEO4JLABS_PLUGINS", "[\"apoc\"]")
    .WithEnvironment("NEO4J_apoc_import_file_enabled", "true")
    .WithHttpEndpoint(port: 7474, targetPort: 7474, name: "http")
    .WithEndpoint(port: 7687, targetPort: 7687, name: "bolt")
    .WithLifetime(ContainerLifetime.Persistent);

var dbDeployment = dreamTravelDb.OnResourceReady(async (resource, @event, ct) =>
{
    var connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct);
    Console.WriteLine($"🔧 Db ConnectionString: {connectionString}");

    var connBuilder = new SqlConnectionStringBuilder(connectionString)
    {
        TrustServerCertificate = true
    };
    connectionString = connBuilder.ToString();

    var dacpacPath = Path.Combine(
        builder.AppHostDirectory,      // .../backend/src/DreamTravel.Aspire
        "..", "..",                    // -> backend/
        "artifacts", "dacpac", "DreamTravelDatabase.dacpac"
    );

    dacpacPath = Path.GetFullPath(dacpacPath);

    if (!File.Exists(dacpacPath))
    {
        throw new FileNotFoundException(
            $"Dacpac not found at {dacpacPath}. Build the database project first: " +
            "dotnet build Infrastructure/DreamTravelDatabase/DreamTravelDatabase.csproj");
    }

    Console.WriteLine($"🔧 Deploying dacpac: {dacpacPath}");

    await SqlProjectDeployer.DeployDacpacAsync(dacpacPath, connectionString, "DreamTravelDatabase", ct);

    Console.WriteLine("✅ Database is ready for connections!");
});

var api = builder.AddProject<Projects.DreamTravel_Api>("dreamtravel-api")
    .WithReference(dreamTravelDb)
    .WithExternalHttpEndpoints()
    .WaitFor(dbDeployment)
    .WaitFor(neo4j);

builder.AddProject<Projects.DreamTravel_Worker>("dreamtravel-worker")
    .WithReference(dreamTravelDb)
    .WaitFor(dbDeployment);

builder.AddProject<Projects.DreamTravel_Ui>("dreamtravel-ui")
    .WithReference(api)
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
