using DreamTravel.DatabaseData.Query.GetPreviewUsers;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays;
using DreamTravel.DatabaseData.Repository.FlightEmailSubscriptions;
using DreamTravel.DatabaseData.Repository.SubscriptionDays;
using DreamTravel.DatabaseData.Repository.Users;
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
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IFlightEmailSubscriptionRepository, FlightEmailSubscriptionRepository>();
            services.AddTransient<ISubscriptionDaysRepository, SubscriptionDaysRepository>();

            services.AddTransient<IGetSubscriptionDetailsByDay, GetSubscriptionDetailsByDay>();
            services.AddTransient<IGetPreviewUsers, GetPreviewUsers>();
            services.AddTransient<IGetSubscriptionsWithDays, GetSubscriptionsWithDaysHandler>();

            return services;
        }
    }
}
