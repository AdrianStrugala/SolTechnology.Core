using BackgroundWorker.Jobs;
using SolTechnology.Core.Scheduler;
using SolTechnology.Core.Scheduler.ScheduleConfig;
using SolTechnology.TaleCode.PlayerRegistry.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLogging(c =>
    c.AddConsole()
        .AddApplicationInsights());
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddCommands();

builder.Services.AddScheduledJob<SynchornizeCristianoRonaldoMatches>(new ScheduleConfig("0 0 * * *")); //every day at midnight


builder.Services.AddControllers();


var app = builder.Build();


app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.Run("http://localhost:0204");
}
app.Run();