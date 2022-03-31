using Cronos;

namespace SolTechnology.Core.Scheduler.ScheduleConfig;

public interface IScheduleConfig<T>
{
    CronExpression CronExpression { get; set; }
    TimeZoneInfo TimeZoneInfo { get; set; }
}