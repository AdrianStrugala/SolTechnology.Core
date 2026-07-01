namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Descriptor for a recurring job registered via <c>AddSolRecurringJob&lt;TJob&gt;</c>.
/// </summary>
internal sealed class RecurringJobDescriptor(Type jobType, string cronExpression, bool preventOverlap = false)
{
    public Type JobType { get; } = jobType;
    public string CronExpression { get; } = cronExpression;
    public bool PreventOverlap { get; } = preventOverlap;
}


