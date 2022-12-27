namespace SolTechnology.Core.Cache
{
    public interface ILazyTaskCache
    {
        Task<TItem> GetOrAdd<TKey, TItem>(TKey key, Func<TKey, Task<TItem>> factory);
    }
}