using System.Collections.Concurrent;

namespace SolTechnology.Core.Cache
{
    public interface IScopedCache<TKey, TItem>
    {
        Task<TItem> GetOrAdd(TKey key, Func<TKey, Task<TItem>> factory);
    }

    public class ScopedCache<TKey, TItem> : IScopedCache<TKey, TItem>
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<TItem>>> _cache = new();

        public async Task<TItem> GetOrAdd(TKey key, Func<TKey, Task<TItem>> factory)
        {
            var keyString = System.Text.Json.JsonSerializer.Serialize(key);
            if (string.IsNullOrWhiteSpace(keyString))
                throw new ArgumentOutOfRangeException(nameof(key), "Cache keys cannot be null or whitespace");

            var lazyItem = _cache.GetOrAdd(keyString, _ =>
                new Lazy<Task<TItem>>(() => factory(key), LazyThreadSafetyMode.ExecutionAndPublication));

            try
            {
                return await lazyItem.Value;
            }
            catch
            {
                _cache.TryRemove(KeyValuePair.Create(keyString, lazyItem));
                throw;
            }
        }

        //it could have generics on method, not class, but it would require Dictionary<object, Task<object>>, which makes casts
    }
}
