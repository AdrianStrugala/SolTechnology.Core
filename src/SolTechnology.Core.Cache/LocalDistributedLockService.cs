using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Cache;

/// <summary>
/// In-process lock for local dev / single-instance scenarios. Keeps one holder entry per key with
/// its expiry deadline — no Redis required. Same contract as <see cref="RedisDistributedLockService"/>:
/// returns <c>null</c> if the lock is already held, never throws on failure, and an expired lock can be
/// taken over; disposing a handle releases the lock only while that handle still holds it.
/// </summary>
internal sealed class LocalDistributedLockService(
    ILogger<LocalDistributedLockService> logger,
    TimeProvider timeProvider) : IDistributedLockService
{
    private readonly ConcurrentDictionary<string, Holder> _locks = new(StringComparer.Ordinal);

    internal int HeldLockCount => _locks.Count;

    public ValueTask<IAsyncDisposable?> TryAcquireLockAsync(
        string name, TimeSpan expiry, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<IAsyncDisposable?>(ct);
        }

        try
        {
            var now = timeProvider.GetUtcNow();
            var holder = new Holder(now + expiry);

            if (!TryClaim(name, holder, now))
            {
                logger.LogDebug("Local lock not acquired: {LockKey} (held by another caller)", name);
                return ValueTask.FromResult<IAsyncDisposable?>(null);
            }

            logger.LogDebug("Local lock acquired: {LockKey}", name);
            return ValueTask.FromResult<IAsyncDisposable?>(new LocalLockHandle(_locks, name, holder, logger));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Local lock acquisition failed for {LockKey} — degrading to null", name);
            return ValueTask.FromResult<IAsyncDisposable?>(null);
        }
    }

    private bool TryClaim(string name, Holder holder, DateTimeOffset now)
    {
        while (true)
        {
            if (_locks.TryAdd(name, holder))
            {
                return true;
            }

            if (!_locks.TryGetValue(name, out var current))
            {
                continue;
            }

            if (current.ExpiresAt > now)
            {
                return false;
            }

            if (_locks.TryUpdate(name, holder, current))
            {
                return true;
            }
        }
    }

    // A class, not a record: release and takeover compare holders by reference.
    private sealed class Holder(DateTimeOffset expiresAt)
    {
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
    }

    private sealed class LocalLockHandle(
        ConcurrentDictionary<string, Holder> locks,
        string name,
        Holder holder,
        ILogger logger) : IAsyncDisposable
    {
        private int _released;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0 && locks.TryRemove(KeyValuePair.Create(name, holder)))
            {
                logger.LogDebug("Local lock released: {LockKey}", name);
            }

            return ValueTask.CompletedTask;
        }
    }
}
