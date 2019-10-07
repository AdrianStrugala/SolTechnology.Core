using System.IO;
using System.Reflection;
using DreamTravel.DatabaseData.FlightEmailOrders;
using DreamTravel.DatabaseData.Subscriptions;
using DreamTravel.DatabaseData.Users;
using DreamTravel.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DatabaseData.Configuration
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDatabaseData(this IServiceCollection services)
        {
            var configurationRoot = new ConfigurationBuilder()
                                    //// SK: have no better idea how to do this. Feel free to tweak it :)
                                    .AddJsonFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\..\\databaseDataSettings.json", true)
                                    .AddJsonFile("databaseDataSettings.json", true)
                                    .Build();

            DatabaseDataConfiguration databaseDataConfiguration = new DatabaseDataConfiguration();
            configurationRoot.Bind(databaseDataConfiguration);
            services.AddSingleton<DatabaseDataConfiguration>(databaseDataConfiguration);

            services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(databaseDataConfiguration.ConnectionString));

            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<ISubscriptionRepository, SubscriptionRepository>();
            services.AddTransient<IFlightEmailOrderRepository, FlightEmailOrderRepository>();

            return services;
        }
    }
}
