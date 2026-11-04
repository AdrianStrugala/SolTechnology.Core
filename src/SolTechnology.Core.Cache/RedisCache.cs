using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Cache;

/// <summary>
/// Redis-backed cache of async results, shared by every instance that uses the same Redis and
/// <see cref="DistributedCacheConfiguration.InstanceName"/>. Items are stored as JSON.
/// </summary>
public interface IRedisCache
{
    /// <summary>
    /// Returns the cached item for <paramref name="key"/> and <typeparamref name="TItem"/>, or runs
    /// <paramref name="factory"/> and caches its result. A Redis failure falls through to the factory.
    /// </summary>
    Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory);
}

internal sealed class RedisCache(
    IDistributedCache cache,
    IOptions<DistributedCacheConfiguration> options,
    ILogger<RedisCache> logger) : IRedisCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly DistributedCacheConfiguration _config = options.Value;

    public async Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory)
    {
        var keyString = CacheKey.For<TKey, TItem>(key, SerializerOptions);

        try
        {
            var bytes = await cache.GetAsync(keyString);
            if (bytes is not null && bytes.Length > 0)
            {
                var cached = JsonSerializer.Deserialize<TItem>(bytes, SerializerOptions);
                if (cached is not null)
                {
                    return cached;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Distributed cache GET failed for key [{CacheKey}] — falling through to factory", keyString);
        }

        var result = await factory(key);

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(result, SerializerOptions);
            var entryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_config.ExpirationSeconds)
            };
            await cache.SetAsync(keyString, bytes, entryOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Distributed cache SET failed for key [{CacheKey}] — skipping", keyString);
        }

        return result;
    }
}
