using System.Collections.Concurrent;
using SolTechnology.Core.Story.Models;

namespace SolTechnology.Core.Story.Persistence;

/// <summary>
/// In-memory implementation of IStoryRepository.
/// Story instances are kept in memory using a thread-safe ConcurrentDictionary.
/// Data is lost when the application restarts.
/// Good for development, testing, and scenarios where persistence is not required across restarts.
/// </summary>
public class InMemoryStoryRepository : IStoryRepository
{
    private readonly ConcurrentDictionary<Auid, StoryInstance> _stories = new();

    /// <summary>
    /// Find a story instance by ID.
    /// Returns a clone to simulate immutability and prevent external modifications.
    /// </summary>
    public Task<StoryInstance?> FindById(Auid storyId)
    {
        if (_stories.TryGetValue(storyId, out var story))
        {
            // Return a clone to simulate database behavior (immutability)
            return Task.FromResult<StoryInstance?>(story.Clone());
        }

        return Task.FromResult<StoryInstance?>(null);
    }

    /// <summary>
    /// Save or update a story instance.
    /// Uses upsert semantics - creates new or updates existing.
    /// Thread-safe via ConcurrentDictionary.
    /// </summary>
    public Task SaveAsync(StoryInstance storyInstance)
    {
        _stories.AddOrUpdate(
            storyInstance.StoryId,
            storyInstance.Clone(),
            (_, _) => storyInstance.Clone());

        return Task.CompletedTask;
    }

    /// <summary>
    /// Delete a story instance.
    /// No-op if the story doesn't exist.
    /// </summary>
    public Task DeleteAsync(Auid storyId)
    {
        _stories.TryRemove(storyId, out _);
        return Task.CompletedTask;
    }
}
