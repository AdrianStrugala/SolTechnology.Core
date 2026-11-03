using System.Collections.Concurrent;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Persistence;

/// <summary>
/// In-memory implementation of <see cref="ITaleRepository"/>.
/// Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>. Data is lost on restart.
/// </summary>
public class InMemoryTaleRepository : ITaleRepository
{
    private readonly ConcurrentDictionary<Auid, TaleInstance> _tales = new();
    private readonly Lock _writeLock = new();

    /// <inheritdoc />
    public Task<TaleInstance?> FindById(Auid taleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_tales.TryGetValue(taleId, out var tale))
            return Task.FromResult<TaleInstance?>(tale.Clone());
        return Task.FromResult<TaleInstance?>(null);
    }

    /// <inheritdoc />
    public Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var match = _tales.Values.FirstOrDefault(s =>
            !string.IsNullOrEmpty(s.IdempotencyKey) &&
            s.IdempotencyKey == idempotencyKey);
        return Task.FromResult(match?.Clone());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TaleInstance>> List(
        TaleStatus? status = null,
        string? handlerTypeName = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<TaleInstance> query = _tales.Values;
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

    /// <inheritdoc />
    public Task<Result> Save(TaleInstance taleInstance, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_writeLock)
        {
            _tales.TryGetValue(taleInstance.TaleId, out var existing);
            if ((existing?.Version ?? 0) != taleInstance.Version)
            {
                return Conflict($"Tale {taleInstance.TaleId} was changed by another writer.");
            }

            if (existing == null && IsIdempotencyKeyTaken(taleInstance))
            {
                return Conflict($"Idempotency key {taleInstance.IdempotencyKey} is already used by another tale.");
            }

            var stored = taleInstance.Clone();
            stored.Version = taleInstance.Version + 1;
            if (existing != null)
            {
                stored.CreatedAt = existing.CreatedAt;
            }

            _tales[taleInstance.TaleId] = stored;
            taleInstance.Version = stored.Version;
        }

        return Result.SuccessAsTask();
    }

    private bool IsIdempotencyKeyTaken(TaleInstance taleInstance) =>
        !string.IsNullOrEmpty(taleInstance.IdempotencyKey) &&
        _tales.Values.Any(t => t.IdempotencyKey == taleInstance.IdempotencyKey);

    private static Task<Result> Conflict(string message) =>
        Result.FailAsTask(new ConflictError { Message = message });

    /// <inheritdoc />
    public Task Delete(Auid taleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _tales.TryRemove(taleId, out _);
        return Task.CompletedTask;
    }
}
