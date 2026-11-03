using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Persistence;

/// <summary>
/// Repository for persisting and retrieving tale instances.
/// Implementations can use in-memory storage, databases, or any other persistence mechanism.
/// </summary>
public interface ITaleRepository
{
    /// <summary>Find a tale instance by its unique identifier. Returns null if not found.</summary>
    Task<TaleInstance?> FindById(Auid taleId, CancellationToken cancellationToken);

    /// <summary>
    /// Find a previously-saved tale with the given caller-supplied idempotency key.
    /// Used by <c>TaleManager.StartStory</c> to deduplicate retried requests.
    /// Returns null if none found.
    /// </summary>
    Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>
    /// Enumerates tale instances matching the filter. Intended for dashboards / operator tooling.
    /// Default implementation throws; override in repositories that want to support listing.
    /// </summary>
    Task<IReadOnlyList<TaleInstance>> List(
        TaleStatus? status = null,
        string? handlerTypeName = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This repository does not support listing.");

    /// <summary>
    /// Conditionally writes a tale instance. The write succeeds only when the stored version (a
    /// missing tale counts as <c>0</c>) equals <see cref="TaleInstance.Version"/>; the stored version
    /// and <see cref="TaleInstance.Version"/> of the passed instance then become <c>Version + 1</c>.
    /// Returns a failed <see cref="Result"/> with a <see cref="Errors.ConflictError"/>, without
    /// writing, on a version mismatch or when a new tale reuses another tale's idempotency key.
    /// Never throws for a conflict.
    /// </summary>
    Task<Result> Save(TaleInstance taleInstance, CancellationToken cancellationToken);

    /// <summary>Delete a tale instance permanently.</summary>
    Task Delete(Auid taleId, CancellationToken cancellationToken);
}
