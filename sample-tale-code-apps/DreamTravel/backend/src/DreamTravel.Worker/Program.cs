using DreamTravel.Trips.Commands;
using DreamTravel.Trips.Sql;
using EntityGraphQL.AspNet;
using Hangfire;

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

        builder.Services.InstallDreamTripsCommands(builder.Configuration);

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