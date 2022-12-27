using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Cache
{
    public class LazyTaskCache : ILazyTaskCache
    {
        private readonly IMemoryCache _memoryCache;
        private readonly CacheConfiguration _cacheConfiguration;

        public LazyTaskCache(IMemoryCache memoryCache, IOptionsMonitor<CacheConfiguration> cacheConfiguration)
        {
            _memoryCache = memoryCache;
            _cacheConfiguration = cacheConfiguration.CurrentValue;
        }

        public Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory)
        {
            var item = _memoryCache.GetOrCreate(key, cacheEntry =>
            {
                if (_cacheConfiguration.ExpirationMode == ExpirationMode.Absolute)
                {
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheConfiguration.ExpirationSeconds));
                }
                else if (_cacheConfiguration.ExpirationMode == ExpirationMode.Sliding)
                {
                    cacheEntry.SetSlidingExpiration(TimeSpan.FromSeconds(_cacheConfiguration.ExpirationSeconds));
                }

                var mode = LazyThreadSafetyMode.ExecutionAndPublication;
                return new Lazy<Task<TItem>>(() => factory(key), mode).Value;
            });

            return item;
        }


        //Another option for the implementation is LazyConcurrentDictionary instead of Memory Cache.
        //It could help with memory and scope management
    }
}
