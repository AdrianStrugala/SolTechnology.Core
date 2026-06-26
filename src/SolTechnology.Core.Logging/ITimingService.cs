namespace SolTechnology.Core.Logging;

/// <summary>
/// Per-request timing diagnostics — records named sub-context durations ("where did the time go?").
/// Scoped service backed by <see cref="AsyncLocal{T}"/> so timing flows with the async request.
/// <para>
/// Usage:
/// <code>
/// using (timingService.StartContext("db")) { await query(); }
/// using (timingService.StartContext("http")) { await callUpstream(); }
/// // On request finish, the aggregated map { "db": 45, "http": 120 } is emitted into the log scope.
/// </code>
/// </para>
/// </summary>
public interface ITimingService
{
    /// <summary>
    /// Starts a named timing context. Disposing the returned handle stops the timer and
    /// accumulates the elapsed time into the named bucket. Multiple calls with the same name
    /// aggregate (sum).
    /// </summary>
    IDisposable StartContext(string name);

    /// <summary>
    /// Returns the aggregated timings as a dictionary of name → elapsed milliseconds.
    /// Called by the logging infrastructure on request/operation finish.
    /// </summary>
    IDictionary<string, long> GetTimings();

    /// <summary>
    /// Resets all accumulated timings. Called at the start of a new scope (e.g. new request).
    /// </summary>
    void Reset();
}

