using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Cache
{
    public interface ISingletonCache
    {
        Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory);
    }

    public class SingletonCache : ISingletonCache
    {
        private readonly IMemoryCache _memoryCache;
        private readonly CacheConfiguration _cacheConfiguration;

        public SingletonCache(IMemoryCache memoryCache, IOptionsMonitor<CacheConfiguration> cacheConfiguration)
        {
            _memoryCache = memoryCache;
            _cacheConfiguration = cacheConfiguration.CurrentValue;
        }

        public Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory)
        {
            var keyString = System.Text.Json.JsonSerializer.Serialize(key);
            if (string.IsNullOrWhiteSpace(keyString))
                throw new ArgumentOutOfRangeException(nameof(key), "Cache keys cannot be null or whitespace");

            if (!_memoryCache.TryGetValue<Lazy<Task<TItem>>>(keyString, out var result))
            {
                var options = BuildEntryOptions();
                result = new Lazy<Task<TItem>>(() => factory(key), LazyThreadSafetyMode.ExecutionAndPublication);
                _memoryCache.Set(keyString, result, options);
            }

            return result!.Value;
        }

        private MemoryCacheEntryOptions BuildEntryOptions()
        {
            var options = new MemoryCacheEntryOptions();
            var expiration = TimeSpan.FromSeconds(_cacheConfiguration.ExpirationSeconds);

            if (_cacheConfiguration.ExpirationMode == ExpirationMode.Sliding)
            {
                options.SetSlidingExpiration(expiration);
            }
            else
            {
                options.SetAbsoluteExpiration(expiration);
            }

            return options;
        }
    }
}
