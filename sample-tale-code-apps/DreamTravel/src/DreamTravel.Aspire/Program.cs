using Microsoft.Data.SqlClient;
using Neo4j.Driver;
using SolTechnology.Core.SQL.SQLProject;

var builder = DistributedApplication.CreateBuilder(args);

var dbPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder
    .AddSqlServer("sql", dbPassword)
    .WithHostPort(1404)
    .WithLifetime(ContainerLifetime.Persistent);

var dreamTravelDb = sql.AddDatabase("DreamTravelDatabase");

var dataPath = Path.Combine(
    builder.AppHostDirectory,      // .../src/DreamTravel.Aspire
    "..", "..",                    // -> DreamTravel/
    "Data"
);
dataPath = Path.GetFullPath(dataPath);

var neo4j = builder.AddContainer("neo4j", "neo4j", "5.10-community")
    .WithEnvironment("NEO4J_AUTH", "neo4j/neo4jpass")
    .WithEnvironment("NEO4J_dbms_directories_import", "/var/lib/neo4j/import")
    .WithEnvironment("NEO4J_dbms_security_procedures_unrestricted", "apoc.*")
    .WithEnvironment("NEO4JLABS_PLUGINS", "[\"apoc\"]")
    .WithEnvironment("NEO4J_apoc_import_file_enabled", "true")
    .WithHttpEndpoint(port: 7474, targetPort: 7474, name: "http")
    .WithEndpoint(port: 7687, targetPort: 7687, name: "bolt")
    .WithBindMount(dataPath, "/var/lib/neo4j/import")
    .WithVolume("neo4j-data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

var neo4jDeployment = neo4j.OnResourceReady(async (resource, @event, ct) =>
{
    Console.WriteLine("🔧 Initializing Neo4j graph database...");

    var uri = "bolt://localhost:7687";
    var username = "neo4j";
    var password = "neo4jpass";

    using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
    await using var session = driver.AsyncSession();

    try
    {
        // Check if graph is already loaded by counting nodes
        var countResult = await session.RunAsync("MATCH (n) RETURN count(n) as count");
        var record = await countResult.SingleAsync();
        var nodeCount = record["count"].As<long>();

        if (nodeCount > 0)
        {
            Console.WriteLine($"✅ Neo4j graph already contains {nodeCount} nodes - skipping import");
            return;
        }

        Console.WriteLine("📥 Loading Wrocław street graph from GraphML...");

        // Import GraphML file using APOC
        await session.RunAsync(
            "CALL apoc.import.graphml('/var/lib/neo4j/import/wroclaw_drive.graphml', " +
            "{readLabels: true, storeNodeIds: true})");

        // Count nodes after import
        var countAfterImport = await session.RunAsync("MATCH (n) RETURN count(n) as count");
        var recordAfterImport = await countAfterImport.SingleAsync();
        var nodesImported = recordAfterImport["count"].As<long>();

        Console.WriteLine($"✅ Neo4j graph loaded successfully! Imported {nodesImported} nodes");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Warning: Failed to initialize Neo4j graph: {ex.Message}");
        Console.WriteLine("Graph data will need to be loaded manually or on next startup");
    }
});

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
        builder.AppHostDirectory,      // .../src/DreamTravel.Aspire
        "..", "..",                    // -> DreamTravel/
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

    await SQLProjectDeployer.DeployDacpacAsync(dacpacPath, connectionString, "DreamTravelDatabase", ct);

    Console.WriteLine("✅ Database is ready for connections!");
});

var api = builder.AddProject<Projects.DreamTravel_Api>("dreamtravel-api")
    .WithReference(dreamTravelDb)
    .WithExternalHttpEndpoints()
    .WaitFor(dbDeployment)
    .WaitFor(neo4jDeployment);

builder.AddProject<Projects.DreamTravel_Worker>("dreamtravel-worker")
    .WithReference(dreamTravelDb)
    .WaitFor(dbDeployment)
    .WaitFor(neo4jDeployment);

builder.AddProject<Projects.DreamTravel_Ui>("dreamtravel-ui")
    .WithReference(api)
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
