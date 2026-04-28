using System.Collections.Concurrent;
using SolTechnology.Core.Story.Models;

namespace SolTechnology.Core.Story.Persistence;

/// <summary>
/// In-memory implementation of <see cref="IStoryRepository"/>.
/// Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>. Data is lost on restart.
/// </summary>
public class InMemoryStoryRepository : IStoryRepository
{
    private readonly ConcurrentDictionary<Auid, StoryInstance> _stories = new();

    public Task<StoryInstance?> FindById(Auid storyId)
    {
        if (_stories.TryGetValue(storyId, out var story))
            return Task.FromResult<StoryInstance?>(story.Clone());
        return Task.FromResult<StoryInstance?>(null);
    }

    public Task<StoryInstance?> FindByIdempotencyKey(string idempotencyKey)
    {
        var match = _stories.Values.FirstOrDefault(s =>
            !string.IsNullOrEmpty(s.IdempotencyKey) &&
            s.IdempotencyKey == idempotencyKey);
        return Task.FromResult(match?.Clone());
    }

    public Task<IReadOnlyList<StoryInstance>> ListAsync(
        StoryStatus? status = null,
        string? handlerTypeName = null,
        int skip = 0,
        int take = 100)
    {
        IEnumerable<StoryInstance> query = _stories.Values;
        if (status.HasValue) query = query.Where(s => s.Status == status.Value);
        if (!string.IsNullOrEmpty(handlerTypeName))
            query = query.Where(s => s.HandlerTypeName == handlerTypeName);

        var result = query
            .OrderByDescending(s => s.LastUpdatedAt)
            .Skip(skip)
            .Take(take)
            .Select(s => s.Clone())
            .ToList();

        return Task.FromResult<IReadOnlyList<StoryInstance>>(result);
    }

    public Task SaveAsync(StoryInstance storyInstance)
    {
        _stories.AddOrUpdate(
            storyInstance.StoryId,
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
