using System.Collections.Concurrent;

namespace SolTechnology.Core.Cache
{
    /// <summary>
    /// Cache of async results that lives as long as the DI scope (for example one HTTP request).
    /// </summary>
    public interface IScopedCache<TKey, TItem>
    {
        /// <summary>
        /// Returns the cached task for <paramref name="key"/>, or runs <paramref name="factory"/> once and caches it.
        /// A faulted or cancelled result is not kept, so the next call runs the factory again.
        /// </summary>
        Task<TItem> GetOrAdd(TKey key, Func<TKey, Task<TItem>> factory);
    }

    /// <summary>Default <see cref="IScopedCache{TKey, TItem}"/>, registered as scoped by <c>AddSolLocalCache</c>.</summary>
    public class ScopedCache<TKey, TItem> : IScopedCache<TKey, TItem>
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<TItem>>> _cache = new();

        /// <inheritdoc />
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
