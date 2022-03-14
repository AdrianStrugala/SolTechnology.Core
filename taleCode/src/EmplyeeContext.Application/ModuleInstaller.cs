using Microsoft.Extensions.DependencyInjection;
using SolTechnology.TaleCode.ApiClients;
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
        public static IServiceCollection AddCommands(this IServiceCollection services)
        {
            services.AddSqlData();
            services.AddApiClients();
            services.AddStaticData();

            services.AddTransient<ICommandHandler<SynchronizePlayerMatchesCommand>, SynchronizePlayerMatchesHandler>();
            services.AddTransient<ISyncPlayer, SyncPlayer>();
            services.AddTransient<IDetermineMatchesToSync, DetermineMatchesToSync>();
            services.AddTransient<ISyncMatch, SyncMatch>();

            services.AddTransient<ICommandHandler<CalculatePlayerStatisticsCommand>, CalculatePlayerStatisticsHandler>();

            return services;
        }
    }
}
