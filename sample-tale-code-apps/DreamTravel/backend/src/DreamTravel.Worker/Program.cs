using DreamTravel.GeolocationData;
using DreamTravel.Infrastructure;
using DreamTravel.Trips.Commands;
using DreamTravel.Trips.GeolocationDataClients;
using DreamTravel.Trips.Sql;
using EntityGraphQL.AspNet;
using Hangfire;
using SolTechnology.Core.Cache;
using SolTechnology.Core.Sql;

namespace DreamTravel.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddLogging(c =>
            c.AddConsole());

        
        var sqlConfiguration = builder.Configuration.GetSection("Sql").Get<SqlConfiguration>()!;
        
        builder.Services.InstallTripsSql(sqlConfiguration);
        builder.Services.InstallGeolocationDataClients();
        builder.Services.InstallInfrastructure();
        builder.Services.InstallDreamTripsCommands();
        
        var cacheConfiguration = builder.Configuration.GetSection("Cache").Get<CacheConfiguration>()!;
        builder.Services.AddCache(cacheConfiguration);

        var thisAssembly = typeof(Program).Assembly;
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(thisAssembly);
        });

        builder.Services.AddHangfireServer();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate("LogFromJob", () => Console.WriteLine("Hello from Job"), Cron.Daily);

        app.MapHangfireDashboard("/hangfire/ui");
        app.MapGraphQL<DreamTripsDbContext>(); // default url: /graphql
        app.MapGraphQLVoyager("voyager/ui");
        app.UseGraphQLPlayground("/graphql/ui");

        app.Run();
    }
}