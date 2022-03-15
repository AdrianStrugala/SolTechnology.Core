using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql;
using SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;

namespace SolTechnology.TaleCode.SqlData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSqlData(this IServiceCollection services)
        {
            services.AddSql();

            services.AddScoped<IPlayerRepository, PlayerRepository>();
            services.AddScoped<IMatchRepository, MatchRepository>();
            services.AddScoped<IExecutionErrorRepository, ExecutionErrorRepository>();

            return services;
        }
    }
}
