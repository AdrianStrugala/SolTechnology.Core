using Cronos;

namespace SolTechnology.Core.Scheduler.ScheduleConfig;

public class ScheduleConfig
{

    public TimeZoneInfo TimeZoneInfo { get; set; }
    public CronExpression CronExpression { get; set; }

    public ScheduleConfig(string cronExpression, TimeZoneInfo timeZoneInfo)
    {
        CronExpression = CronExpression.Parse(cronExpression);
        TimeZoneInfo = timeZoneInfo;
    }

    public ScheduleConfig(CronExpression cronExpression, TimeZoneInfo timeZoneInfo)
    {
        CronExpression = cronExpression;
        TimeZoneInfo = timeZoneInfo;
    }

    public ScheduleConfig(CronExpression cronExpression)
    {
        CronExpression = cronExpression;
        TimeZoneInfo = TimeZoneInfo.Utc;
    }
    public ScheduleConfig(string cronExpression)
    {
        CronExpression = CronExpression.Parse(cronExpression);
        TimeZoneInfo = TimeZoneInfo.Utc;
    }
}