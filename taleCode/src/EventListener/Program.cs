using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.MessageBus;
using SolTechnology.TaleCode.EventListener.PlayerMatchesSynchronized;
using SolTechnology.TaleCode.PlayerRegistry.Commands;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLogging(c =>
            c.AddConsole()
                .AddApplicationInsights());
        // services.AddApplicationInsightsTelemetry();

        services.AddCommands();

        services.AddMessageBus()
           .WithReceiver<PlayerMatchesSynchronizedEvent, CalculatePlayerStatistics>("synchronizeplayermatches", "calculatestatistics");
    });
// var builder = WebApplication.CreateBuilder(args);

// Add services to the container.







var app = builder.Build();
await app.RunAsync();


// app.Run("http://localhost:2137");
