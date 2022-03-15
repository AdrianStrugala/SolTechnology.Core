using Microsoft.Extensions.DependencyInjection;
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
        public static IServiceCollection AddCommands(this IServiceCollection services)
        {
            services.AddSqlData();
            services.AddApiClients();
            services.AddStaticData();
            services.AddBlobData();

            services.AddScoped<ICommandHandler<SynchronizePlayerMatchesCommand>, SynchronizePlayerMatchesHandler>();
            services.AddScoped<ISyncPlayer, SyncPlayer>();
            services.AddScoped<IDetermineMatchesToSync, DetermineMatchesToSync>();
            services.AddScoped<ISyncMatch, SyncMatch>();

            services.AddScoped<ICommandHandler<CalculatePlayerStatisticsCommand>, CalculatePlayerStatisticsHandler>();

            return services;
        }
    }
}
