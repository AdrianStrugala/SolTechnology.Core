using SolTechnology.Core.MessageBus;
using SolTechnology.TaleCode.EventListener.PlayerMatchesSynchronized;
using SolTechnology.TaleCode.PlayerRegistry.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLogging(c =>
            c.AddConsole()
                .AddApplicationInsights());
// services.AddApplicationInsightsTelemetry();

builder.Services.AddCommands();

builder.Services.AddMessageBus()
           .WithReceiver<PlayerMatchesSynchronizedEvent, CalculatePlayerStatistics>("synchronizeplayermatches", "calculatestatistics");

builder.Services.AddControllers();


var app = builder.Build();


app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.Run("http://localhost:2137");
}
app.Run();

