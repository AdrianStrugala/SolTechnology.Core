using DreamTravel.DatabaseData.Users;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DatabaseData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddDatabaseData(this IServiceCollection services, string connectionString)
        {
            services.AddTransient<IUserRepository, UserRepository>();

            return services;
        }
    }
}
