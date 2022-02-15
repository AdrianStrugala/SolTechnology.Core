using Microsoft.Extensions.DependencyInjection;
using SolTechnology.TaleCode.Domain.Match;
using SolTechnology.TaleCode.Domain.Player;
using SolTechnology.TaleCode.SqlData.Repository.Match;
using SolTechnology.TaleCode.SqlData.Repository.Player;

namespace SolTechnology.TaleCode.SqlData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSqlData(this IServiceCollection services)
        {
            services.AddTransient<IPlayerRepository, PlayerRepository>();
            services.AddTransient<IMatchRepository, MatchRepository>();

            return services;
        }
    }
}
