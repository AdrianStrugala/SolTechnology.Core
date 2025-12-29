using SolTechnology.Core.Story.Models;

namespace SolTechnology.Core.Story.Persistence;

/// <summary>
/// Repository for persisting and retrieving story instances.
/// Implementations can use in-memory storage, databases, or any other persistence mechanism.
/// </summary>
public interface IStoryRepository
{
    /// <summary>
    /// Find a story instance by its unique identifier.
    /// Returns null if the story is not found.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story</param>
    /// <returns>The story instance if found, null otherwise</returns>
    Task<StoryInstance?> FindById(Auid storyId);

    /// <summary>
    /// Save or update a story instance.
    /// If the story already exists (same StoryId), it should be updated.
    /// If it's new, it should be inserted.
    /// </summary>
    /// <param name="storyInstance">The story instance to save</param>
    Task SaveAsync(StoryInstance storyInstance);

    /// <summary>
    /// Delete a story instance permanently.
    /// </summary>
    /// <param name="storyId">The unique identifier of the story to delete</param>
    Task DeleteAsync(Auid storyId);
}
