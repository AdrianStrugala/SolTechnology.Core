using System.Collections.Concurrent;
using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Persistence;

/// <summary>
/// In-memory implementation of <see cref="ITaleRepository"/>.
/// Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>. Data is lost on restart.
/// </summary>
public class InMemoryTaleRepository : ITaleRepository
{
    private readonly ConcurrentDictionary<Auid, TaleInstance> _stories = new();

    public Task<TaleInstance?> FindById(Auid storyId)
    {
        if (_stories.TryGetValue(storyId, out var story))
            return Task.FromResult<TaleInstance?>(story.Clone());
        return Task.FromResult<TaleInstance?>(null);
    }

    public Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey)
    {
        var match = _stories.Values.FirstOrDefault(s =>
            !string.IsNullOrEmpty(s.IdempotencyKey) &&
            s.IdempotencyKey == idempotencyKey);
        return Task.FromResult(match?.Clone());
    }

    public Task<IReadOnlyList<TaleInstance>> ListAsync(
        TaleStatus? status = null,
        string? handlerTypeName = null,
        int skip = 0,
        int take = 100)
    {
        IEnumerable<TaleInstance> query = _stories.Values;
        if (status.HasValue) query = query.Where(s => s.Status == status.Value);
        if (!string.IsNullOrEmpty(handlerTypeName))
            query = query.Where(s => s.HandlerTypeName == handlerTypeName);

        var result = query
            .OrderByDescending(s => s.LastUpdatedAt)
            .Skip(skip)
            .Take(take)
            .Select(s => s.Clone())
            .ToList();

        return Task.FromResult<IReadOnlyList<TaleInstance>>(result);
    }

    public Task SaveAsync(TaleInstance storyInstance)
    {
        _stories.AddOrUpdate(
            storyInstance.TaleId,
            storyInstance.Clone(),
            (_, existing) =>
            {
                var updated = storyInstance.Clone();
                // Preserve CreatedAt from the original record (review §2.2).
                updated.CreatedAt = existing.CreatedAt;
                return updated;
            });

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Auid storyId)
    {
        _stories.TryRemove(storyId, out _);
        return Task.CompletedTask;
    }
}
