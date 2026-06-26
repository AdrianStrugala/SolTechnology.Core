using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Cache;

/// <summary>
/// In-process lock for local dev / single-instance scenarios. Uses <see cref="SemaphoreSlim"/>
/// per key — no Redis required. Same contract as <see cref="RedisDistributedLockService"/>:
/// returns <c>null</c> if the lock is already held, never throws on failure.
/// </summary>
internal sealed class LocalDistributedLockService(ILogger<LocalDistributedLockService> logger) : IDistributedLockService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async ValueTask<IAsyncDisposable?> TryAcquireLockAsync(
        string name, TimeSpan expiry, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var semaphore = _locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));

        try
        {
            var acquired = await semaphore.WaitAsync(TimeSpan.Zero, ct);

            if (!acquired)
            {
                logger.LogDebug("Local lock not acquired: {LockKey} (held by another caller)", name);
                return null;
            }

            logger.LogDebug("Local lock acquired: {LockKey}", name);
            return new LocalLockHandle(semaphore, name, logger);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Local lock acquisition failed for {LockKey} — degrading to null", name);
            return null;
        }
    }

    private sealed class LocalLockHandle(SemaphoreSlim semaphore, string name, ILogger logger) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            logger.LogDebug("Local lock released: {LockKey}", name);
            return ValueTask.CompletedTask;
        }
    }
}

