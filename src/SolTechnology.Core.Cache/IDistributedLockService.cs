namespace SolTechnology.Core.Cache;

/// <summary>
/// Distributed mutual exclusion — ensures only one instance holds a named lock at any given time.
/// <para>
/// Contract: <see cref="TryAcquireLock"/> returns a disposable handle on success, <c>null</c>
/// on failure. It <b>never throws</b> for a backend/timeout failure — only an out-of-range expiry
/// (<see cref="ArgumentOutOfRangeException"/>) and caller-cancellation
/// (<see cref="OperationCanceledException"/>) surface as exceptions.
/// </para>
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Attempts to acquire a distributed lock with the given <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Logical lock name. Should include tenant/principal where relevant.</param>
    /// <param name="expiry">
    /// How long the lock is held before auto-release (prevents deadlocks on crash). Must be greater than zero and at
    /// most 365 days.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A disposable handle if the lock was acquired — disposing releases it.
    /// <c>null</c> if someone else holds the lock or the backend is unavailable.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expiry"/> is zero, negative, or longer than 365 days.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled.</exception>
    /// <returns>
    /// A disposable handle if the lock was acquired — disposing releases it.
    /// <c>null</c> if someone else holds the lock or the backend is unavailable.
    /// </returns>
    ValueTask<IAsyncDisposable?> TryAcquireLock(string name, TimeSpan expiry, CancellationToken ct = default);
}

