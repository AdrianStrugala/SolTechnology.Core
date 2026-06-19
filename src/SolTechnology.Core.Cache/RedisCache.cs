using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Cache;

public interface IRedisCache
{
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
        var keyString = JsonSerializer.Serialize(key, SerializerOptions);
        if (string.IsNullOrWhiteSpace(keyString))
            throw new ArgumentOutOfRangeException(nameof(key), "Cache keys cannot be null or whitespace");

        try
        {
            var bytes = await cache.GetAsync(keyString);
            if (bytes is not null && bytes.Length > 0)
            {
                var cached = JsonSerializer.Deserialize<TItem>(bytes, SerializerOptions);
                if (cached is not null) return cached;
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
