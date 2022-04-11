namespace SolTechnology.Core.Scheduler.Configuration;

public interface ISchedulerConfigurationProvider
{
    ScheduledJobConfiguration ResolveScheduledJobConfig(string jobName);
    void RegisterScheduledJobConfig(string jobName, ScheduledJobConfiguration scheduledJobConfiguration);
}