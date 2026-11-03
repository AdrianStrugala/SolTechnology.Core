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
    /// How long a <see cref="Models.TaleStatus.Running"/> tale stays owned by the run that last saved
    /// it. <c>ResumeStory</c> refuses a younger Running tale with a conflict and takes over an older
    /// one, left by a crashed host, re-running its unfinished chapter. A live run renews its lease
    /// every third of this value while it executes, so a chapter may run longer. Default: 5 minutes.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public TimeSpan RunningLeaseTimeout
    {
        get => _runningLeaseTimeout;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            _runningLeaseTimeout = value;
        }
    }

    /// <summary>
    /// Internal fallback used by the engine when options are not present in DI (e.g. when a
    /// <see cref="TaleHandler{TInput,TContext,TOutput}"/> is resolved from a container that
    /// does not call <c>AddSolTale</c>). Application code should not depend on this.
    /// </summary>
    internal static TaleOptions Default { get; } = new();
}
