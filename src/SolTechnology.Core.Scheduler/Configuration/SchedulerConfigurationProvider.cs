using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Scheduler.Configuration;

class SchedulerConfigurationProvider : ISchedulerConfigurationProvider
{
    private readonly ILogger<SchedulerConfigurationProvider> _logger;
    private static readonly Dictionary<string, ScheduledJobConfiguration> JobToConfigMap = new();

    public SchedulerConfigurationProvider(ILogger<SchedulerConfigurationProvider> logger)
    {
        _logger = logger;
    }

    public ScheduledJobConfiguration ResolveScheduledJobConfig(string jobName)
    {
        if (!JobToConfigMap.TryGetValue(jobName, out var config))
        {
            throw new ArgumentException($"Scheduler for job type: [{jobName}] is not configured.");
        }

        return config;
    }

    public void RegisterScheduledJobConfig(string jobName, ScheduledJobConfiguration scheduledJobConfiguration)
    {
        JobToConfigMap[jobName] = scheduledJobConfiguration;

        _logger.LogInformation($"Registered scheduler configuration for job [{jobName}]");
    }
}