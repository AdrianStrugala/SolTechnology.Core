using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache;

/// <summary>
/// Redis-backed distributed lock using <c>SET NX EX</c> (atomic "set if not exists" with expiry).
/// Acquiring returns a handle whose <see cref="IAsyncDisposable.DisposeAsync"/> releases the lock
/// via <c>DEL</c> (only if the value still matches — prevents releasing someone else's lock after
/// expiry).
/// </summary>
internal sealed class RedisDistributedLockService(
    IConnectionMultiplexer redis,
    IOptions<DistributedCacheConfiguration> options,
    ILogger<RedisDistributedLockService> logger) : IDistributedLockService
{
    private readonly string _keyPrefix = options.Value.InstanceName + "lock:";
    private readonly RedisConnectionGate _gate = new(redis, logger);

    public async ValueTask<IAsyncDisposable?> TryAcquireLock(
        string name, TimeSpan expiry, CancellationToken ct = default)
    {
        LockExpiry.Validate(expiry);
        ct.ThrowIfCancellationRequested();

        var key = _keyPrefix + name;
        if (_gate.IsClosed(nameof(TryAcquireLock), key))
        {
            return null;
        }

        var value = Guid.NewGuid().ToString("N"); // unique per acquisition — fencing token

        try
        {
            var db = redis.GetDatabase();
            var acquired = await db.StringSetAsync(key, value, expiry, When.NotExists);

            if (!acquired)
            {
                logger.LogDebug("Lock not acquired: [{LockKey}] (held by another instance)", key);
                return null;
            }

            logger.LogDebug("Lock acquired: [{LockKey}]", key);
            return new LockHandle(db, key, value, logger);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Guard-rail: never throw into host loop on backend failure — degrade to null.
            logger.LogWarning(ex, "Lock acquisition failed for [{LockKey}] — degrading to null", key);
            return null;
        }
    }

    /// <summary>
    /// Release handle — deletes the key only if the value still matches (our fencing token).
    /// This prevents releasing a lock that expired and was re-acquired by another instance; such a
    /// release logs a warning. Disposing twice releases once.
    /// </summary>
    private sealed class LockHandle(IDatabase db, string key, string value, ILogger logger) : IAsyncDisposable
    {
        // Lua script: atomic "delete if value matches"
        private const string ReleaseScript = """
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end
            """;

        private int _released;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 1)
            {
                return;
            }

            try
            {
                var deleted = (int)await db.ScriptEvaluateAsync(ReleaseScript, [(RedisKey)key], [(RedisValue)value]);
                if (deleted == 1)
                {
                    logger.LogDebug("Lock released: [{LockKey}]", key);
                }
                else
                {
                    logger.LogWarning("Lock [{LockKey}] expired before release", key);
                }
            }
            catch (Exception ex)
            {
                // Best-effort release — lock will expire anyway via TTL.
                logger.LogWarning(ex, "Lock release failed for [{LockKey}] — will expire via TTL", key);
            }
        }
    }
}

