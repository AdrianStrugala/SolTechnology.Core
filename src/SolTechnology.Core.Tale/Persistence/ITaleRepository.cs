using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Persistence;

/// <summary>
/// Repository for persisting and retrieving story instances.
/// Implementations can use in-memory storage, databases, or any other persistence mechanism.
/// </summary>
public interface ITaleRepository
{
    /// <summary>Find a story instance by its unique identifier. Returns null if not found.</summary>
    Task<TaleInstance?> FindById(Auid storyId);

    /// <summary>
    /// Find a previously-saved story with the given caller-supplied idempotency key.
    /// Used by <c>TaleManager.StartStory</c> to deduplicate retried requests.
    /// Returns null if none found.
    /// </summary>
    Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey);

    /// <summary>
    /// Enumerates story instances matching the filter. Intended for dashboards / operator tooling.
    /// Default implementation throws; override in repositories that want to support listing.
    /// </summary>
    Task<IReadOnlyList<TaleInstance>> ListAsync(
        TaleStatus? status = null,
        string? handlerTypeName = null,
        int skip = 0,
        int take = 100)
        => throw new NotSupportedException("This repository does not support listing.");

    /// <summary>Save or update a story instance (upsert).</summary>
    Task SaveAsync(TaleInstance storyInstance);

    /// <summary>Delete a story instance permanently.</summary>
    Task DeleteAsync(Auid storyId);
}
