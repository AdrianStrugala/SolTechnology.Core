using Microsoft.Extensions.DependencyInjection;
using SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;

namespace SolTechnology.TaleCode.SqlData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSqlData(this IServiceCollection services)
        {
            services.AddTransient<IPlayerRepository, PlayerRepository>();
            services.AddTransient<IMatchRepository, MatchRepository>();
            services.AddTransient<IExecutionErrorRepository, ExecutionErrorRepository>();

            return services;
        }
    }
}
