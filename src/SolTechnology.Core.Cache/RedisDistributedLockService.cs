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
internal sealed class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLockService> _logger;
    private readonly string _keyPrefix;

    public RedisDistributedLockService(
        IConnectionMultiplexer redis,
        IOptions<DistributedCacheConfiguration> options,
        ILogger<RedisDistributedLockService> logger)
    {
        _redis = redis;
        _logger = logger;
        _keyPrefix = options.Value.InstanceName + "lock:";
    }

    public async ValueTask<IAsyncDisposable?> TryAcquireLockAsync(
        string name, TimeSpan expiry, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = _keyPrefix + name;
        var value = Guid.NewGuid().ToString("N"); // unique per acquisition — fencing token

        try
        {
            var db = _redis.GetDatabase();
            var acquired = await db.StringSetAsync(key, value, expiry, When.NotExists);

            if (!acquired)
            {
                _logger.LogDebug("Lock not acquired: {LockKey} (held by another instance)", key);
                return null;
            }

            _logger.LogDebug("Lock acquired: {LockKey}", key);
            return new LockHandle(db, key, value, _logger);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Guard-rail: never throw into host loop on backend failure — degrade to null.
            _logger.LogWarning(ex, "Lock acquisition failed for {LockKey} — degrading to null", key);
            return null;
        }
    }

    /// <summary>
    /// Release handle — deletes the key only if the value still matches (our fencing token).
    /// This prevents releasing a lock that expired and was re-acquired by another instance.
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

        public async ValueTask DisposeAsync()
        {
            try
            {
                await db.ScriptEvaluateAsync(ReleaseScript, [(RedisKey)key], [(RedisValue)value]);
                logger.LogDebug("Lock released: {LockKey}", key);
            }
            catch (Exception ex)
            {
                // Best-effort release — lock will expire anyway via TTL.
                logger.LogWarning(ex, "Lock release failed for {LockKey} — will expire via TTL", key);
            }
        }
    }
}

