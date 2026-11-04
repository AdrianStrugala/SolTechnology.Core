namespace SolTechnology.Core.Cache;

/// <summary>
/// Redis settings for <c>AddSolDistributedCache</c>, shared by <see cref="IRedisCache"/>, the Redis lock, and the
/// Redis idempotency store. Validated at host start.
/// </summary>
public class DistributedCacheConfiguration
{
    /// <summary>StackExchange.Redis connection string; must not be empty.</summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// Prefix of every Redis key this package writes: cache entries, <c>lock:</c> keys, and <c>idempotency:</c> keys.
    /// Must not be empty; <c>"SolTechnology:"</c> keeps the 1.x lock and idempotency keys.
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>Absolute lifetime of an <see cref="IRedisCache"/> entry in seconds (default 300); must be greater than 0.</summary>
    public int ExpirationSeconds { get; set; } = 5 * 60;
}

