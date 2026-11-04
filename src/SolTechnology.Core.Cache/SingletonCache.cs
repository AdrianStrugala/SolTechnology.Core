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
        private readonly Lock _gate = new();

        public SingletonCache(IMemoryCache memoryCache, IOptionsMonitor<CacheConfiguration> cacheConfiguration)
        {
            _memoryCache = memoryCache;
            _cacheConfiguration = cacheConfiguration.CurrentValue;
        }

        public async Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory)
        {
            var keyString = CacheKey.For<TKey, TItem>(key);
            var entry = GetOrCreateEntry(keyString, () => factory(key));

            try
            {
                return await entry.Value;
            }
            catch
            {
                RemoveIfCurrent(keyString, entry);
                throw;
            }
        }

        private Lazy<Task<TItem>> GetOrCreateEntry<TItem>(string keyString, Func<Task<TItem>> factory)
        {
            lock (_gate)
            {
                if (_memoryCache.TryGetValue<Lazy<Task<TItem>>>(keyString, out var existing) && existing is not null)
                {
                    return existing;
                }

                var created = new Lazy<Task<TItem>>(factory, LazyThreadSafetyMode.ExecutionAndPublication);
                _memoryCache.Set(keyString, created, BuildEntryOptions());
                return created;
            }
        }

        private void RemoveIfCurrent<TItem>(string keyString, Lazy<Task<TItem>> entry)
        {
            lock (_gate)
            {
                if (_memoryCache.TryGetValue(keyString, out var current) && ReferenceEquals(current, entry))
                {
                    _memoryCache.Remove(keyString);
                }
            }
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
