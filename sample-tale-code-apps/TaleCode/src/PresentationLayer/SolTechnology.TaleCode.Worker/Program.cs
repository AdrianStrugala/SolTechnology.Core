using Hangfire;
using SolTechnology.Core.Api.Middlewares;
using SolTechnology.Core.Logging.Middleware;
using SolTechnology.Core.Sql;
using SolTechnology.TaleCode.PlayerRegistry.Commands;

namespace SolTechnology.TaleCode.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();


        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddLogging(c =>
            c.AddConsole()
                .AddApplicationInsights());
        builder.Services.AddApplicationInsightsTelemetry();

        builder.Services.InstallCommands();

// builder.Services.AddScheduledJob<SynchornizeCristianoRonaldoMatches>(new ScheduledJobConfiguration("0 0 * * *")); //every day at midnight
//
// builder.Services.AddMessageBus()
//     .WithQueueReceiver<PlayerMatchesSynchronizedEvent, CalculatePlayerStatistics>();

//HANGFIRE
        var sqlConnectionString = builder.Configuration.GetSection("Configuration:Sql").Get<SqlConfiguration>().ConnectionString;
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(sqlConnectionString));

        builder.Services.AddHangfireServer();


        var app = builder.Build();

        app.MapDefaultEndpoints();



        app.UseExceptionHandler("/error");
        app.UseMiddleware<LoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlerMiddleware>();


        app.UseHttpsRedirection();
        app.MapHangfireDashboard();


        app.Run();
    }
}