using Microsoft.Extensions.DependencyInjection;
using SolTechnology.TaleCode.Domain.Player;
using SolTechnology.TaleCode.SqlData.Repository;

namespace SolTechnology.TaleCode.SqlData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSqlData(this IServiceCollection services)
        {
            services.AddTransient<IPlayerRepository, PlayerRepository>();

            return services;
        }
    }
}
