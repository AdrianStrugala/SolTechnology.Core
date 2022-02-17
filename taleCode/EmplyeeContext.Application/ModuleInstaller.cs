using ApiClients;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using SolTechnology.TaleCode.SqlData;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddCommands(this IServiceCollection services)
        {
            services.AddSqlData();
            services.AddApiClients();

            services.AddTransient<ICommandHandler<SynchronizePlayerMatchesCommand>, SynchronizePlayerMatchesHandler>();
            services.AddTransient<IBuildPlayer, BuildPlayer>();
            services.AddTransient<IBuildMatch, BuildMatch>();

            return services;
        }
    }
}
