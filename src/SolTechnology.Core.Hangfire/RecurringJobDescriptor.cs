namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Descriptor for a recurring job registered via <c>AddRecurringJob&lt;TJob&gt;</c>.
/// </summary>
internal sealed class RecurringJobDescriptor(Type jobType, string cronExpression)
{
    public Type JobType { get; } = jobType;
    public string CronExpression { get; } = cronExpression;
}

