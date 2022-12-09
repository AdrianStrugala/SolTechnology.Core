using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.MessageBus;
using SolTechnology.TaleCode.ApiClients;
using SolTechnology.TaleCode.BlobData;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.SqlData;
using SolTechnology.TaleCode.StaticData;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallCommands(this IServiceCollection services)
        {
            services.InstallSql();
            services.InstallApiClients();
            services.InstallStaticData();
            services.InstallBlobStorage();
            services.AddMessageBus()
                    .WithQueuePublisher<PlayerMatchesSynchronizedEvent>();


            services.AddScoped<ISyncPlayer, SyncPlayer>();
            services.AddScoped<IDetermineMatchesToSync, DetermineMatchesToSync>();
            services.AddScoped<ISyncMatch, SyncMatch>();

            services.AddScoped<CalculatePlayerStatisticsHandler>();

            services.AddScoped<ICommandHandler<CalculatePlayerStatisticsCommand>>(x =>
                new CommandHandlerLoggingDecorator<CalculatePlayerStatisticsCommand>(
                    x.GetService<CalculatePlayerStatisticsHandler>()!,
                    x.GetService<ILogger<ICommandHandler<CalculatePlayerStatisticsCommand>>>()!));


            //TODO: Add Decoration to separate library (preferably Logger)
            services.AddScoped<SynchronizePlayerMatchesHandler>();

            services.AddScoped<ICommandHandler<SynchronizePlayerMatchesCommand>>(x =>
                new CommandHandlerLoggingDecorator<SynchronizePlayerMatchesCommand>(
                    x.GetService<SynchronizePlayerMatchesHandler>()!,
                    x.GetService<ILogger<ICommandHandler<SynchronizePlayerMatchesCommand>>>()!));


            // services.AddScoped<ICommandHandler<SynchronizePlayerMatchesCommand>, SynchronizePlayerMatchesHandler>();

            return services;
        }
    }
}
