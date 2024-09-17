using Hangfire;
using SolTechnology.Core.Api.Middlewares;
using SolTechnology.Core.Logging.Middleware;
using SolTechnology.Core.Sql;
using SolTechnology.TaleCode.PlayerRegistry.Commands;
using SolTechnology.TaleCode.Worker.ScheduledJobs;

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

        builder.Services.InstallCommands(builder.Configuration);

        //HANGFIRE
        var sqlConnectionString = builder.Configuration.GetSection("Configuration:Sql").Get<SqlConfiguration>().ConnectionString;
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(sqlConnectionString));

        builder.Services.AddHangfireServer();


        var app = builder.Build();


        var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<SynchornizeCristianoRonaldoMatches>(
            nameof(SynchornizeCristianoRonaldoMatches),
            x => x.Execute(),
            Cron.Daily);


        app.MapDefaultEndpoints();

        app.UseExceptionHandler("/error");
        app.UseMiddleware<LoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlerMiddleware>();


        app.UseHttpsRedirection();
        app.MapHangfireDashboard();

        app.Run();
    }
}