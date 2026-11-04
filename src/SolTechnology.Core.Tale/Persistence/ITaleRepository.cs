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
    /// <para>
    /// Every property must be stored and returned exactly, in particular <see cref="TaleInstance.Version"/>
    /// and <see cref="TaleInstance.LeaseExpiresAt"/> (the same instant, nullable, on inserts and updates
    /// alike, never replaced by a database default or shifted by a time-zone conversion): the Running
    /// lease is decided from it. Once per host the run reads a claimed or renewed tale back and logs an
    /// error when the stored expiry differs from the written one.
    /// A save whose outcome is unknown (it may have committed) must throw; never retry a conditional
    /// write that may have committed, and never report the caller's own write as a conflict, because
    /// the run would read that as a takeover by another run.
    /// </para>
    /// <para>
    /// Lease renewals call this method while a chapter runs, through an instance resolved from a fresh
    /// DI scope, so a scoped repository (for example over a <c>DbContext</c>) is never used by a
    /// renewal and a chapter of the same scope at once.
    /// </para>
    /// </summary>
    Task<Result> Save(TaleInstance taleInstance, CancellationToken cancellationToken);

    /// <summary>Delete a tale instance permanently.</summary>
    Task Delete(Auid taleId, CancellationToken cancellationToken);
}
