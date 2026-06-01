using System.Diagnostics;

namespace SolTechnology.Core.Testing;

/// <summary>
/// Polls an operation until a condition is met — the canonical way to assert against
/// eventually-consistent state (queues, projections, async handlers) in component / integration
/// tests. Replaces the three divergent copies that lived in MTS, KYC and the sample apps.
/// </summary>
public static class Retry
{
    /// <summary>
    /// Invokes <paramref name="action"/> up to <paramref name="maxAttempts"/> times, returning the
    /// first result that satisfies <paramref name="condition"/>. Throws <see cref="TimeoutException"/>
    /// if the condition is never met.
    /// </summary>
    public static async Task<T> UntilConditionMet<T>(
        Func<Task<T>> action,
        Func<T, bool> condition,
        int maxAttempts,
        TimeSpan pauseInterval,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(condition);

        var attempts = 0;
        do
        {
            var result = await action().ConfigureAwait(false);
            attempts++;

            if (condition(result))
            {
                return result;
            }

            await Task.Delay(pauseInterval, ct).ConfigureAwait(false);
        } while (attempts < maxAttempts);

        throw new TimeoutException(
            $"The operation did not satisfy the condition after {maxAttempts} attempts.");
    }

    /// <summary>
    /// Synchronous-source overload: wraps a non-async <paramref name="action"/> in the same
    /// attempt/poll loop. Useful for probes that are themselves synchronous (e.g. a DB query helper).
    /// </summary>
    public static Task<T> UntilConditionMet<T>(
        Func<T> action,
        Func<T, bool> condition,
        int maxAttempts,
        TimeSpan pauseInterval,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return UntilConditionMet(() => Task.FromResult(action()), condition, maxAttempts, pauseInterval, ct);
    }

    /// <summary>
    /// Time-budget variant: keeps invoking <paramref name="action"/> until the condition is met or
    /// <paramref name="totalWaitTime"/> elapses, then returns the last result (does not throw).
    /// </summary>
    public static async Task<T> UntilConditionMetOrTimeout<T>(
        Func<Task<T>> action,
        Func<T, bool> condition,
        TimeSpan totalWaitTime,
        TimeSpan pauseInterval,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(condition);

        var stopwatch = Stopwatch.StartNew();
        T result;
        do
        {
            result = await action().ConfigureAwait(false);
            if (condition(result))
            {
                return result;
            }

            await Task.Delay(pauseInterval, ct).ConfigureAwait(false);
        } while (stopwatch.Elapsed < totalWaitTime);

        return result;
    }
}

