namespace SolTechnology.Core.Tale;

/// <summary>
/// Engine-level policies for the Tale framework. Does not own persistence — the repository
/// is chosen and registered independently through the <see cref="Builder.ITaleBuilder"/>
/// returned by <c>AddSolTale(...)</c> (e.g. <c>UseInMemoryTaleRepository</c>,
/// <c>UseTaleRepository&lt;T&gt;</c>). A repository is
/// always present — in-memory is the default.
/// </summary>
public sealed class TaleOptions
{
    private TimeSpan _runningLeaseTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Prefix used when generating new <see cref="SolTechnology.Core.AUID.Auid"/> tale identifiers. Use distinct
    /// prefixes to separate tenants or environments in a shared persistence store.
    /// </summary>
    public string TaleIdPrefix { get; set; } = "STR";

    /// <summary>
    /// How long a <see cref="Models.TaleStatus.Running"/> tale stays owned by the run that claimed or
    /// last renewed it. Each claim and renewal stores the expiry as
    /// <see cref="Models.TaleInstance.LeaseExpiresAt"/> (the writer's clock plus this value), so hosts
    /// with different values still respect each other's leases. <c>ResumeStory</c> refuses a Running
    /// tale before that expiry with a conflict and takes it over afterwards, re-running its unfinished
    /// chapter. A live run renews its lease every third of this value while it executes, so a chapter
    /// may run longer; a chapter running for three times this value is logged as a warning.
    /// Default: 5 minutes; range: 1 second to 49 days, which keeps the renewal timer below the
    /// <see cref="ITimer"/> limit of 4294967294 ms (about 49.7 days).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 1 second or above 49 days.</exception>
    public TimeSpan RunningLeaseTimeout
    {
        get => _runningLeaseTimeout;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinRunningLeaseTimeout);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxRunningLeaseTimeout);
            _runningLeaseTimeout = value;
        }
    }

    internal static TimeSpan MinRunningLeaseTimeout { get; } = TimeSpan.FromSeconds(1);

    internal static TimeSpan MaxRunningLeaseTimeout { get; } = TimeSpan.FromDays(49);

    /// <summary>
    /// Internal fallback used by the engine when options are not present in DI (e.g. when a
    /// <see cref="TaleHandler{TInput,TContext,TOutput}"/> is resolved from a container that
    /// does not call <c>AddSolTale</c>). Application code should not depend on this.
    /// </summary>
    internal static TaleOptions Default { get; } = new();
}
