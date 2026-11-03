using System.Text.Json;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Reads and writes the persisted state of one tale run through <see cref="ITaleRepository"/>,
/// carrying the optimistic-concurrency version and the idempotency key between saves.
/// </summary>
internal sealed class TaleStatePersistence(ITaleRepository repository, TaleRunState state)
{
    private DateTime _createdAt;
    private long _version;
    private string? _idempotencyKey;

    public void Start(string? idempotencyKey)
    {
        _createdAt = state.Now;
        _idempotencyKey = idempotencyKey;
    }

    public void Restore(TaleInstance instance)
    {
        state.Restore(instance);
        _createdAt = instance.CreatedAt;
        _version = instance.Version;
        _idempotencyKey = instance.IdempotencyKey;
    }

    public async Task Save<TContext>(
        TContext context,
        Auid taleId,
        string handlerTypeName,
        TaleStatus status,
        CancellationToken cancellationToken)
    {
        if (taleId == Auid.Empty)
        {
            return;
        }

        var instance = new TaleInstance
        {
            TaleId = taleId,
            HandlerTypeName = handlerTypeName,
            Status = status,
            CreatedAt = _createdAt == default ? state.Now : _createdAt,
            LastUpdatedAt = state.Now,
            History = new List<ChapterInfo>(state.History),
            CurrentChapter = status == TaleStatus.WaitingForInput ? state.History.LastOrDefault() : null,
            Context = JsonSerializer.Serialize(context, TaleJsonOptions.Default),
            IdempotencyKey = _idempotencyKey,
            Version = _version
        };

        var saved = await repository.Save(instance, cancellationToken);
        if (saved.IsFailure)
        {
            state.SaveError = saved.Error;
            return;
        }

        _version = instance.Version;
    }
}
