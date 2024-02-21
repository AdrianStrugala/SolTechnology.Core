using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Cache;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.MessageBus;
using SolTechnology.TaleCode.ApiClients;
using SolTechnology.TaleCode.BlobData;
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
            services.RegisterCommands();

            services.InstallSql();
            services.InstallApiClients();
            services.InstallStaticData();
            services.InstallBlobStorage();
            services.AddMessageBus()
                    .WithQueuePublisher<PlayerMatchesSynchronizedEvent>();
            services.AddCache();


            services.AddScoped<ISyncPlayer, SyncPlayer>();
            services.AddScoped<IDetermineMatchesToSync, DetermineMatchesToSync>();
            services.AddScoped<ISyncMatches, SyncMatches>();


            return services;
        }
    }
}
