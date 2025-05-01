using DreamTravel.GraphDatabase;
using DreamTravel.Infrastructure;
using DreamTravel.Trips.Commands;
using DreamTravel.Trips.GeolocationDataClients;
using DreamTravel.Trips.Sql;
using DreamTravel.Worker.BackgroundJobs;
using EntityGraphQL.AspNet;
using Hangfire;
using SolTechnology.Core.Cache;
using SolTechnology.Core.Sql;
using System.Globalization;

namespace DreamTravel.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        builder.AddServiceDefaults();
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddLogging(c =>
            c.AddConsole());

        
        //SQL
        var sqlConfiguration = builder.Configuration.GetSection("Sql").Get<SqlConfiguration>()!;
        builder.Services.InstallTripsSql(sqlConfiguration);
        
        //Graph
        builder.Services.Configure<Neo4jSettings>(
            builder.Configuration.GetSection("Neo4j"));
        builder.Services.InstallGraphDatabase();
        
        builder.Services.InstallGeolocationDataClients();
        builder.Services.InstallInfrastructure();
        builder.Services.InstallDreamTripsCommands();
        
        //CACHE
        var cacheConfiguration = builder.Configuration.GetSection("Cache").Get<CacheConfiguration>()!;
        builder.Services.AddCache(cacheConfiguration);

        //MEDIATR
        var thisAssembly = typeof(Program).Assembly;
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(thisAssembly);
        });

        builder.Services.AddHangfireServer();

        //APP
        var app = builder.Build();

        app.MapDefaultEndpoints();

        var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate("LogFromJob", () => Console.WriteLine("Hello from Job"), Cron.Daily);


        FetchTrafficJob.Register();

        app.MapHangfireDashboard("/hangfire/ui");
        app.MapGraphQL<DreamTripsDbContext>(); // default url: /graphql
        app.MapGraphQLVoyager("voyager/ui");
        app.UseGraphQLPlayground("/graphql/ui");

        app.Run();
    }


}