namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Descriptor for a recurring job registered via <c>AddSolRecurringJob&lt;TJob&gt;</c>.
/// </summary>
internal sealed class RecurringJobDescriptor(
    Type jobType,
    string cronExpression,
    bool preventOverlap = false,
    string? jobId = null,
    TimeZoneInfo? timeZone = null)
{
    public Type JobType { get; } = jobType;
    public string CronExpression { get; } = cronExpression;
    public bool PreventOverlap { get; } = preventOverlap;
    public string JobId { get; } = jobId ?? StorageKey.RecurringJobId(jobType);
    public TimeZoneInfo TimeZone { get; } = timeZone ?? TimeZoneInfo.Utc;

    /// <summary>The id 1.x registered the job under.</summary>
    public string LegacyJobId => JobType.Name;

    public Type RunnerType => typeof(RecurringJobRunner<>).MakeGenericType(JobType);
}
