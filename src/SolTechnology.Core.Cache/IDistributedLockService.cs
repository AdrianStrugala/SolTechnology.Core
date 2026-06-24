namespace SolTechnology.Core.Cache;

/// <summary>
/// Distributed mutual exclusion — ensures only one instance holds a named lock at any given time.
/// <para>
/// Contract: <see cref="TryAcquireLockAsync"/> returns a disposable handle on success, <c>null</c>
/// on failure. It <b>never throws</b> for a backend/timeout failure — only caller-cancellation may
/// surface as <see cref="OperationCanceledException"/>.
/// </para>
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Attempts to acquire a distributed lock with the given <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Logical lock name. Should include tenant/principal where relevant.</param>
    /// <param name="expiry">How long the lock is held before auto-release (prevents deadlocks on crash).</param>
    /// <param name="ct">Cancellation token — cancellation is the only reason this method may throw.</param>
    /// <returns>
    /// A disposable handle if the lock was acquired — disposing releases it.
    /// <c>null</c> if someone else holds the lock or the backend is unavailable.
    /// </returns>
    ValueTask<IAsyncDisposable?> TryAcquireLockAsync(string name, TimeSpan expiry, CancellationToken ct = default);
}

