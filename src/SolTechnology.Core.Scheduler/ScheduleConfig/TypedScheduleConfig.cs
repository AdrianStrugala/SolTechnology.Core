using Cronos;

namespace SolTechnology.Core.Scheduler.ScheduleConfig;

public class TypedScheduleConfig<T> : IScheduleConfig<T>
{

    public TimeZoneInfo TimeZoneInfo { get; set; }
    public CronExpression CronExpression { get; set; }

    public TypedScheduleConfig(CronExpression cronExpression, TimeZoneInfo timeZoneInfo)
    {
        CronExpression = cronExpression;
        TimeZoneInfo = timeZoneInfo;
    }
}