using DreamTravel.Identity.DatabaseData.Repositories.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql;

namespace DreamTravel.Identity.DatabaseData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallIdentityDatabaseData(this IServiceCollection services, SqlConfiguration sqlConfiguration)
        {
            services.AddSql(sqlConfiguration);

            services.AddTransient<IUserRepository, UserRepository>();

            return services;
        }
    }
}
