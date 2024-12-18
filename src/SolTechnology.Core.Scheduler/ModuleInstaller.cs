using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Scheduler.Configuration;

namespace SolTechnology.Core.Scheduler
{

    public static class ModuleInstaller
    {
        public static IServiceCollection AddScheduledJob<T>(
            this IServiceCollection services,
            ScheduledJobConfiguration scheduledJobConfiguration = null)
            where T : ScheduledJob
        {
            var jobName = typeof(T).Name;

            services
                .AddOptions<List<ScheduledJobConfiguration>>()
                .Configure<IConfiguration>((config, configuration) =>
                {
                    if (scheduledJobConfiguration != null && string.IsNullOrEmpty(scheduledJobConfiguration.JobName))
                    {
                        scheduledJobConfiguration.JobName = jobName;
                    }
                    if (scheduledJobConfiguration == null)
                    {
                        var appSettingsscheduledJobConfiguration = configuration.GetSection("SolTechnology:ScheduledJobs").Get<List<AppSettingsScheduledJobConfiguration>>().FirstOrDefault(a => a.JobName.Equals(jobName, StringComparison.InvariantCultureIgnoreCase));

                        if (appSettingsscheduledJobConfiguration == null)
                        {
                            throw new ArgumentException($"The [{nameof(ScheduledJobConfiguration)}] for job: [{jobName}] is missing. Provide it by parameter or configuration section");
                        }
                        scheduledJobConfiguration = new ScheduledJobConfiguration(appSettingsscheduledJobConfiguration.JobName, appSettingsscheduledJobConfiguration.CronExpression);
                    }

                    config.Add(scheduledJobConfiguration);
                });

            var options = services.BuildServiceProvider().GetRequiredService<IOptions<List<ScheduledJobConfiguration>>>().Value;

            scheduledJobConfiguration = options.First(h => h.JobName.Equals(jobName, StringComparison.InvariantCultureIgnoreCase));

            if (scheduledJobConfiguration.CronExpression == null)
            {
                throw new ArgumentNullException(nameof(ScheduledJobConfiguration.CronExpression), @"Empty Cron Expression is not allowed.");
            }

            var configurationProvider = ResolveSchedulerConfigProvider(services);

            configurationProvider.RegisterScheduledJobConfig(jobName, scheduledJobConfiguration);

            services.AddScoped<T>();
            services.AddHostedService<T>();
            return services;
        }

        private static ISchedulerConfigurationProvider ResolveSchedulerConfigProvider(IServiceCollection services)
        {
            var configurationProvider = services.BuildServiceProvider()
                .GetService<ISchedulerConfigurationProvider>();
            if (configurationProvider != null)
            {
                return configurationProvider;
            }

            services.AddSingleton<ISchedulerConfigurationProvider, SchedulerConfigurationProvider>();
            configurationProvider = services.BuildServiceProvider()
                .GetRequiredService<ISchedulerConfigurationProvider>();

            return configurationProvider;
        }
    }
}