using DreamTravel.DatabaseData.FlightEmailSubscriptions;
using DreamTravel.DatabaseData.Query.GetPreviewUsers;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.DatabaseData.SubscriptionDays;
using DreamTravel.DatabaseData.Users;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Domain.Users;
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
            services.AddTransient<IFlightEmailSubscriptionRepository, FlightEmailSubscriptionRepository>();
            services.AddTransient<ISubscriptionDaysRepository, SubscriptionDaysRepository>();

            services.AddTransient<IGetSubscriptionDetailsByDay, GetSubscriptionDetailsByDay>();
            services.AddTransient<IGetPreviewUsers, GetPreviewUsers>();

            return services;
        }
    }
}
