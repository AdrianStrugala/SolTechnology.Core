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
/// <para>
/// The <c>CancellationToken</c> parameters are accepted for interface symmetry but are best-effort:
/// the underlying <c>StackExchange.Redis</c> APIs do not take a token, so a call already dispatched
/// to Redis is not cancelled mid-flight.
/// </para>
/// </summary>
internal sealed class RedisIdempotencyStore(
    IConnectionMultiplexer redis,
    IOptions<DistributedCacheConfiguration> options,
    ILogger<RedisIdempotencyStore> logger,
    TimeSpan ttl) : IIdempotencyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _keyPrefix = options.Value.InstanceName + "idempotency:";

    public async Task<bool> TryAdd(string key, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            // SET NX EX — atomic "reserve if not exists" with TTL.
            // Value starts as empty (placeholder) — SetResponse overwrites with the full response.
            return await db.StringSetAsync(FullKey(key), "", ttl, When.NotExists);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Idempotency TryAdd failed for {Key} — allowing request through", key);
            return true; // fail-open: let the request through rather than blocking forever
        }
    }

    public async Task<StoredResponse?> Get(string key, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(FullKey(key));

            // IsNullOrEmpty also covers the empty placeholder written by TryAdd —
            // key reserved but the response has not been persisted yet.
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<StoredResponse>((string)value!, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Idempotency Get failed for {Key}", key);
            return null;
        }
    }

    public async Task SetResponse(string key, StoredResponse response, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var json = JsonSerializer.Serialize(response, JsonOptions);
            await db.StringSetAsync(FullKey(key), json, ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Idempotency SetResponse failed for {Key} — response will not be replayed on retry", key);
        }
    }

    public async Task Remove(string key, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(FullKey(key));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Idempotency Remove failed for {Key} — key will expire via TTL", key);
        }
    }

    private string FullKey(string key) => _keyPrefix + key;
}

