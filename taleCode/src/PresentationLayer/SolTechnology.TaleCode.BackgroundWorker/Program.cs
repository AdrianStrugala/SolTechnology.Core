using SolTechnology.Core.MessageBus;
using SolTechnology.Core.Scheduler;
using SolTechnology.Core.Scheduler.Configuration;
using SolTechnology.TaleCode.BackgroundWorker.EventHandlers.OnPlayerMatchesSynchronized;
using SolTechnology.TaleCode.BackgroundWorker.ScheduledJobs;
using SolTechnology.TaleCode.PlayerRegistry.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLogging(c =>
    c.AddConsole()
        .AddApplicationInsights());
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.InstallCommands();

builder.Services.AddScheduledJob<SynchornizeCristianoRonaldoMatches>(new ScheduledJobConfiguration("0 0 * * *")); //every day at midnight

builder.Services.AddControllers();

builder.Services.AddMessageBus()
    .WithQueueReceiver<PlayerMatchesSynchronizedEvent, CalculatePlayerStatistics>();

var app = builder.Build();


app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.Run("http://localhost:0204");
}
app.Run();