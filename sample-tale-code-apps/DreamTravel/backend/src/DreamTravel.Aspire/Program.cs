using Microsoft.Data.SqlClient;
using SolTechnology.Core.Sql.SqlProject;

var builder = DistributedApplication.CreateBuilder(args);

var dbPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder
    .AddSqlServer("sql", dbPassword)
    .WithHostPort(1404)
    .WithLifetime(ContainerLifetime.Persistent);

var dreamTravelDb = sql.AddDatabase("DreamTravelDatabase");

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
    .WaitFor(dbDeployment);

builder.AddProject<Projects.DreamTravel_Worker>("dreamtravel-worker")
    .WithReference(dreamTravelDb)
    .WaitFor(dbDeployment);

builder.AddProject<Projects.DreamTravel_Ui>("dreamtravel-ui")
    .WithReference(api)
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
