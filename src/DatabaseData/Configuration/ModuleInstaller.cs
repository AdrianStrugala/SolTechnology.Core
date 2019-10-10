using DreamTravel.DatabaseData.FlightEmailOrders;
using DreamTravel.DatabaseData.Users;
using DreamTravel.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DatabaseData.Configuration
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDatabaseData(this IServiceCollection services)
        {
            DatabaseDataConfiguration databaseDataConfiguration = new DatabaseDataConfiguration();
            services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(databaseDataConfiguration.ConnectionString));

            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IFlightEmailOrderRepository, FlightEmailOrderRepository>();

            return services;
        }
    }
}
