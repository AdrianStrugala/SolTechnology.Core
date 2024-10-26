using DreamTravel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Infrastructure
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallInfrastructure(this IServiceCollection services)
        {
            services.AddTransient<IHangfireNotificationPublisher, HangfireNotificationPublisher>();

            return services;
        }
    }
}
