using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Scheduler.ScheduleConfig;

namespace SolTechnology.Core.Scheduler
{

    public static class ModuleInstaller
    {
        public static IServiceCollection AddScheduledJob<T>(this IServiceCollection services, ScheduleConfig.ScheduleConfig config) where T : ScheduledJob
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), @"Please provide Schedule Configurations.");
            }
            if (config.CronExpression == null)
            {
                throw new ArgumentNullException(nameof(ScheduleConfig.ScheduleConfig.CronExpression), @"Empty Cron Expression is not allowed.");
            }

            services.AddSingleton<IScheduleConfig<T>>(new TypedScheduleConfig<T>(config.CronExpression, config.TimeZoneInfo));
            services.AddHostedService<T>();
            return services;
        }
    }
}