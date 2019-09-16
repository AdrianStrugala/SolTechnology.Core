using DreamTravel.DatabaseData.Subscriptions;
using DreamTravel.DatabaseData.Users;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.DatabaseData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddDatabaseData(this IServiceCollection services)
        {
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<ISubscriptionRepository, SubscriptionRepository>();

            return services;
        }
    }
}
