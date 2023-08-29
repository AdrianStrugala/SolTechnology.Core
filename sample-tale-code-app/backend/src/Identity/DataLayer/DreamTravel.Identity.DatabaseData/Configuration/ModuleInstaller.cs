using DreamTravel.Domain.Users;
using DreamTravel.Identity.DatabaseData.Repository.Users;
using DreamTravel.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Identity.DatabaseData.Configuration
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDatabaseData(this IServiceCollection services)
        {
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

            services.AddTransient<IUserRepository, UserRepository>();

            return services;
        }
    }
}
