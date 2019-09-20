using System.IO;
using DreamTravel.DatabaseData.FlightEmailOrders;
using DreamTravel.DatabaseData.Subscriptions;
using DreamTravel.DatabaseData.Users;
using DreamTravel.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace DreamTravel.DatabaseData.Configuration
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDatabaseData(this IServiceCollection services)
        {
            DatabaseDataConfiguration databaseDataConfiguration = JsonConvert.DeserializeObject<DatabaseDataConfiguration>(File.ReadAllText("databaseDataSettings.json"));
            services.AddSingleton<DatabaseDataConfiguration>(databaseDataConfiguration);
            services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(databaseDataConfiguration.ConnectionString));

            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<ISubscriptionRepository, SubscriptionRepository>();
            services.AddTransient<IFlightEmailOrderRepository, FlightEmailOrderRepository>();

            return services;
        }
    }
}
