using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Cache
{
    public interface ISingletonCache
    {
        Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory, MemoryCacheEntryOptions? memoryCacheEntryOptions = null);
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

        public Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory,
            MemoryCacheEntryOptions? memoryCacheEntryOptions = null)
        {
            var keyString = System.Text.Json.JsonSerializer.Serialize(key);
            if (string.IsNullOrWhiteSpace(keyString))
                throw new ArgumentOutOfRangeException(nameof(key), "Cache keys cannot be null or whitespace");

            if (!_memoryCache.TryGetValue<Lazy<Task<TItem>>>(keyString, out var result))
            {
                var entryOptions = new MemoryCacheEntryOptions();
                if (memoryCacheEntryOptions == null)
                {
                    //Apply values from CacheConfiguration if not specified for the entry
                    if (_cacheConfiguration.ExpirationMode == ExpirationMode.Absolute)
                    {
                        entryOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheConfiguration.ExpirationSeconds));
                    }
                    else if (_cacheConfiguration.ExpirationMode == ExpirationMode.Sliding)
                    {
                        entryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(_cacheConfiguration.ExpirationSeconds));
                    }
                }
                else
                {
                    //Override CacheConfiguration value with provided argument
                    entryOptions = memoryCacheEntryOptions;
                }
               
                var mode = LazyThreadSafetyMode.ExecutionAndPublication;
                result = new Lazy<Task<TItem>>(() => factory(key), mode);
                
                _memoryCache.Set(keyString, result, entryOptions);
            }

            return result!.Value;
        }
    }
}
