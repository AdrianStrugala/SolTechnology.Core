using ApiClients;
using ApiClients.FootballDataApi;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;
using SolTechnology.TaleCode.SqlData;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddCommands(this IServiceCollection services)
        {
            services.AddSqlData();
            services.AddApiClients();

            services.AddScoped<ICommandHandler<SynchronizePlayerMatchesCommand>, SynchronizePlayerMatchesHandler>();

            return services;
        }
    }
}
