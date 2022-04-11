using Cronos;

namespace SolTechnology.Core.Scheduler.Configuration;

public class AppSettingsScheduledJobConfiguration
{
    public string CronExpression { get; set; }
    public string JobName { get; set; }
}