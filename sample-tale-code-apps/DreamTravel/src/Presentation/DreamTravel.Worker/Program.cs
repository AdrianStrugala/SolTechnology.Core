using DreamTravel.GraphDatabase;
using DreamTravel.Infrastructure;
using DreamTravel.Commands;
using DreamTravel.GeolocationDataClients;
using DreamTravel.Sql;
using DreamTravel.Worker.BackgroundJobs;
using EntityGraphQL.AspNet;
using Hangfire;
using SolTechnology.Core.Cache;
using SolTechnology.Core.SQL;
using System.Globalization;
using DreamTravel.DomainServices;
using DreamTravel.ServiceDefaults;

namespace DreamTravel.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var cultureInfo = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        builder.AddServiceDefaults();
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddLogging(c =>
            c.AddConsole());

        
        //INSTALL MODULES
        var sqlConfiguration = builder.Configuration.GetSection("Sql").Get<SQLConfiguration>()!;
        builder.Services.InstallTripsSql(sqlConfiguration);
        builder.Services.InstallGeolocationDataClients();
        builder.Services.InstallInfrastructure();
        builder.Services.AddHangfireSmartRetry();
        builder.Services.InstallDomainServices();
        builder.Services.InstallDreamTripsCommands();
        
        //Graph
        builder.Services.Configure<Neo4jSettings>(
            builder.Configuration.GetSection("Neo4j"));
        builder.Services.InstallGraphDatabase();
        
        //CACHE
        var cacheConfiguration = builder.Configuration.GetSection("Cache").Get<CacheConfiguration>()!;
        builder.Services.AddCache(cacheConfiguration);

        //MEDIATR
        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<Program>());

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