using DreamTravel.Infrastructure.Events;
using DreamTravel.Infrastructure.Hangfire;
using global::Hangfire;
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

        /// <summary>
        /// Adds Hangfire smart retry filters with configurable delays.
        /// Default delays: 10s, 1m, 5m, 30m, 1h, 2h, 4h, 8h, 16h, 24h
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="delaysInSeconds">Optional custom retry delays in seconds.</param>
        public static IServiceCollection AddHangfireSmartRetry(this IServiceCollection services, int[]? delaysInSeconds = null)
        {
            var delays = delaysInSeconds ?? [10, 60, 300, 1800, 3600, 7200, 14400, 28800, 57600, 86400];

            GlobalJobFilters.Filters.Add(new HangfireSmartRetryAttribute(delays));
            GlobalJobFilters.Filters.Add(new HangfireCorrelationIdFilter());

            return services;
        }
    }
}
