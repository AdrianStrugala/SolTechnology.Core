using SolTechnology.Core.Sql.SqlProject;

var builder = DistributedApplication.CreateBuilder(args);

var dbPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder
    .AddSqlServer("sql", dbPassword)
    .WithHostPort(1404)
    .WithLifetime(ContainerLifetime.Persistent);

var dreamTravelDb = sql.AddDatabase("DreamTravelDatabase");

dreamTravelDb.OnResourceReady(async (resource, @event, ct) =>
{
    var connectionString = await resource.ConnectionStringExpression.GetValueAsync(ct);
    var sqlProjPath = Path.Combine(
        builder.AppHostDirectory, 
        "..", "..", "db", "DreamTravelDatabase", "DreamTravelDatabase.sqlproj"
    );

    await SqlProjectDeployer.DeployAsync(
        sqlProjPath, 
        connectionString, 
        "DreamTravelDatabase", 
        ct
    );
});

builder.AddProject<Projects.DreamTravel_Api>("dreamtravel-api");

builder.AddProject<Projects.DreamTravel_Worker>("dreamtravel-worker")
    .WaitFor(sql);

builder.AddProject<Projects.DreamTravel_Ui>("dreamtravel-ui");

builder.Build().Run();
