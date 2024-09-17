using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql;
using SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository;
using SolTechnology.TaleCode.SqlData.Repository.MatchRepository;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;

namespace SolTechnology.TaleCode.SqlData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallSql(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSql();

            var sqlConfiguration =
                configuration.GetSection(nameof(SqlConfiguration)).Get<SqlConfiguration>();
            services.AddDbContext<TaleCodeDbContext>(options =>
                options.UseSqlServer(sqlConfiguration.ConnectionString));
            
            services.AddScoped<IPlayerRepository, PlayerRepositoryOnEf>();
            services.AddScoped<IMatchRepository, MatchRepository>();
            services.AddScoped<IExecutionErrorRepository, ExecutionErrorRepository>();

            return services;
        }
    }
}
