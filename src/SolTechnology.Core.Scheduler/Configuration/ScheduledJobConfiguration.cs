using Cronos;

namespace SolTechnology.Core.Scheduler.Configuration;

public class ScheduledJobConfiguration
{
    public string JobName { get; set; }
    public TimeZoneInfo TimeZoneInfo { get; set; }
    public CronExpression CronExpression { get; }


    public ScheduledJobConfiguration(string jobName, string cronExpression)
    {
        JobName = jobName;
        TimeZoneInfo = TimeZoneInfo.Utc;
        CronExpression = CronExpression.Parse(cronExpression);
    }

    public ScheduledJobConfiguration(string cronExpression, TimeZoneInfo timeZoneInfo)
    {
        CronExpression = CronExpression.Parse(cronExpression);
        TimeZoneInfo = timeZoneInfo;
    }

    public ScheduledJobConfiguration(string cronExpression, TimeZoneInfo timeZoneInfo, string jobName)
    {
        CronExpression = CronExpression.Parse(cronExpression);
        TimeZoneInfo = timeZoneInfo;
        JobName = jobName;
    }

    public ScheduledJobConfiguration(CronExpression cronExpression, TimeZoneInfo timeZoneInfo, string jobName)
    {
        CronExpression = cronExpression;
        TimeZoneInfo = timeZoneInfo;
        JobName = jobName;
    }

    public ScheduledJobConfiguration(CronExpression cronExpression, TimeZoneInfo timeZoneInfo)
    {
        CronExpression = cronExpression;
        TimeZoneInfo = timeZoneInfo;
    }

    public ScheduledJobConfiguration(CronExpression cronExpression)
    {
        CronExpression = cronExpression;
        TimeZoneInfo = TimeZoneInfo.Utc;
    }
    public ScheduledJobConfiguration(string cronExpression)
    {
        CronExpression = CronExpression.Parse(cronExpression);
        TimeZoneInfo = TimeZoneInfo.Utc;
    }
}