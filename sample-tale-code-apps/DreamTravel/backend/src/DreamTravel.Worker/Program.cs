using DreamTravel.Trips.Commands;
using DreamTravel.Worker.EventHandlers.OnCitySearched;
using Hangfire;
using SolTechnology.Core.Sql;

namespace DreamTravel.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

     
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddLogging(c =>
            c.AddConsole());

        builder.Services.InstallDreamTripsCommands(builder.Configuration);

        //HANGFIRE
        builder.Services.AddHangfireServer();
        
        var thisAssembly = typeof(Program).Assembly;
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(thisAssembly);
        });


        var app = builder.Build();

        var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate("LogFromJob", () => Console.WriteLine("Hello from Job"), Cron.Daily);

        app.MapHangfireDashboard();

        app.Run();
    }
}