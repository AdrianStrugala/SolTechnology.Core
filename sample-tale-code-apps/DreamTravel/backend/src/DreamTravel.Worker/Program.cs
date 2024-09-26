using DreamTravel.Trips.Commands;
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
        var sqlConnectionString = builder.Configuration.GetSection("Configuration:Sql").Get<SqlConfiguration>().ConnectionString;
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(sqlConnectionString));

        builder.Services.AddHangfireServer();
        
        var thisAssembly = typeof(Program).Assembly;
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(thisAssembly);
        });


        var app = builder.Build();


        // var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
        // recurringJobManager.AddOrUpdate<SynchornizeCristianoRonaldoMatches>(
        //     nameof(SynchornizeCristianoRonaldoMatches),
        //     x => x.Execute(),
        //     Cron.Daily);


        app.MapHangfireDashboard();

        app.Run();
    }
}