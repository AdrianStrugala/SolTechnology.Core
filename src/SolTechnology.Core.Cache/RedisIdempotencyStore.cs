using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache;

/// <summary>
/// Redis-backed idempotency store. Uses <c>SET NX EX</c> for atomic key reservation and stores
/// the full <see cref="StoredResponse"/> as a JSON value. Multi-instance safe — two pods racing
/// the same key: only one wins the <c>NX</c> (the other gets <c>false</c> from TryAdd and must
/// replay or wait).
/// </summary>
internal sealed class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisIdempotencyStore> _logger;
    private readonly string _keyPrefix;
    private readonly TimeSpan _ttl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisIdempotencyStore(
        IConnectionMultiplexer redis,
        IOptions<DistributedCacheConfiguration> options,
        ILogger<RedisIdempotencyStore> logger,
        TimeSpan ttl)
    {
        _redis = redis;
        _logger = logger;
        _keyPrefix = options.Value.InstanceName + "idempotency:";
        _ttl = ttl;
    }

    public async Task<bool> TryAddAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            // SET NX EX — atomic "reserve if not exists" with TTL.
            // Value starts as empty (placeholder) — SetResponseAsync overwrites with the full response.
            return await db.StringSetAsync(FullKey(key), "", _ttl, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency TryAdd failed for {Key} — allowing request through", key);
            return true; // fail-open: let the request through rather than blocking forever
        }
    }

    public async Task<StoredResponse?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(FullKey(key));

            if (value.IsNullOrEmpty || value == "")
                return null; // Key reserved but response not yet persisted.

            return JsonSerializer.Deserialize<StoredResponse>((string)value!, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency Get failed for {Key}", key);
            return null;
        }
    }

    public async Task SetResponseAsync(string key, StoredResponse response, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(response, JsonOptions);
            await db.StringSetAsync(FullKey(key), json, _ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency SetResponse failed for {Key} — response will not be replayed on retry", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(FullKey(key));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency Remove failed for {Key} — key will expire via TTL", key);
        }
    }

    private string FullKey(string key) => _keyPrefix + key;
}

