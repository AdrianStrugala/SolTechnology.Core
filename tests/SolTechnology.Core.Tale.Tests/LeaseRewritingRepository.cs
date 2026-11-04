using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// A custom repository that breaks the <see cref="TaleInstance.LeaseExpiresAt"/> contract: it stores the
/// expiry chosen by <c>storedLease</c> (given the written instance and the row it replaces) instead of
/// the written one, and can read back a newer version than the last save stored.
/// </summary>
public sealed class LeaseRewritingRepository(
    InMemoryTaleRepository inner,
    Func<TaleInstance, TaleInstance?, DateTimeOffset?> storedLease) : ITaleRepository
{
    public bool ReadsNewerVersion { get; init; }

    public async Task<TaleInstance?> FindById(Auid taleId, CancellationToken cancellationToken)
    {
        var tale = await inner.FindById(taleId, cancellationToken);
        if (tale != null && ReadsNewerVersion)
        {
            tale.Version++;
        }

        return tale;
    }

    public Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken)
        => inner.FindByIdempotencyKey(idempotencyKey, cancellationToken);

    public Task<IReadOnlyList<TaleInstance>> List(
        TaleStatus? status = null,
        string? handlerTypeName = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
        => inner.List(status, handlerTypeName, skip, take, cancellationToken);

    public async Task<Result> Save(TaleInstance taleInstance, CancellationToken cancellationToken)
    {
        var existing = await inner.FindById(taleInstance.TaleId, cancellationToken);
        var stored = taleInstance.Clone();
        stored.LeaseExpiresAt = storedLease(taleInstance, existing);
        var saved = await inner.Save(stored, cancellationToken);
        taleInstance.Version = stored.Version;
        return saved;
    }

    public Task Delete(Auid taleId, CancellationToken cancellationToken) => inner.Delete(taleId, cancellationToken);
}
