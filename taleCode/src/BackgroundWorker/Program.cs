using SolTechnology.Core.MessageBus;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddEndpointsApiExplorer();
        services.AddLogging(c =>
            c.AddConsole()
                .AddApplicationInsights());
        services.AddApplicationInsightsTelemetry();
    });
// var builder = WebApplication.CreateBuilder(args);

// Add services to the container.







var app = builder.Build();
await app.StartAsync();


// app.Run("http://localhost:2137");
