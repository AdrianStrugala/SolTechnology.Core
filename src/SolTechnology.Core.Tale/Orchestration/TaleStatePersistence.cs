using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Reads and writes the persisted state of one tale run, carrying the optimistic-concurrency version and the
/// idempotency key between saves. A Running save stamps <see cref="TaleInstance.LeaseExpiresAt"/>; any other status
/// clears it. Saves and lease renewals take turns on the version the previous one stored; a renewal overlaps the
/// chapter, so it saves through a fresh DI scope. Claims and renewals are offered to <see cref="LeaseReadBackCheck"/>.
/// </summary>
internal sealed class TaleStatePersistence(
    ITaleRepository repository,
    TaleRunState state,
    TimeSpan runningLeaseTimeout,
    IServiceScopeFactory renewalScopes,
    LeaseReadBackCheck? readBack,
    ILogger logger)
{
    private readonly SemaphoreSlim _turn = new(1, 1);
    private DateTime _createdAt;
    private long _version;
    private string? _idempotencyKey;
    private TaleInstance? _lease;
    private bool _claimed;

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
        if (taleId == Auid.Empty || state.SaveError != null)
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
            LeaseExpiresAt = status == TaleStatus.Running ? state.UtcNow + runningLeaseTimeout : null,
            History = new List<ChapterInfo>(state.History),
            CurrentChapter = status == TaleStatus.WaitingForInput ? state.History.LastOrDefault() : null,
            Context = JsonSerializer.Serialize(context, TaleJsonOptions.Default),
            IdempotencyKey = _idempotencyKey
        };

        await _turn.WaitAsync(cancellationToken);
        try
        {
            await Store(instance, cancellationToken);
        }
        finally
        {
            _turn.Release();
        }
    }

    /// <summary>
    /// Re-saves the last stored Running state with a fresh <see cref="TaleInstance.LeaseExpiresAt"/>.
    /// A <see cref="ConflictError"/> means another run owns the tale now; it stops every later save.
    /// </summary>
    public async Task<Result> Renew(CancellationToken cancellationToken)
    {
        await _turn.WaitAsync(cancellationToken);
        try
        {
            if (_lease == null || state.SaveError != null)
            {
                return Result.Success();
            }

            _lease.LeaseExpiresAt = state.UtcNow + runningLeaseTimeout;
            _lease.Version = _version;
            await using var scope = renewalScopes.CreateAsyncScope();
            var renewalRepository = scope.ServiceProvider.GetRequiredService<ITaleRepository>();
            var renewed = await renewalRepository.Save(_lease, cancellationToken);
            if (renewed.IsSuccess)
            {
                _version = _lease.Version;
                await ReadBack(renewalRepository, _lease, LeaseWrite.Renewal, cancellationToken);
            }
            else if (renewed.Error is ConflictError)
            {
                state.SaveError = renewed.Error;
            }

            return renewed;
        }
        finally
        {
            _turn.Release();
        }
    }

    private async Task Store(TaleInstance instance, CancellationToken cancellationToken)
    {
        // A lease renewal may have lost the tale to another run while this save waited for its turn.
        if (state.SaveError != null)
        {
            return;
        }

        var write = _version == 0 ? LeaseWrite.ClaimByInsert : LeaseWrite.ClaimByUpdate;
        instance.Version = _version;
        var saved = await repository.Save(instance, cancellationToken);
        if (saved.IsFailure)
        {
            state.SaveError = saved.Error;
            return;
        }

        _version = instance.Version;
        if (!_claimed)
        {
            _claimed = true;
            await ReadBack(repository, instance, write, cancellationToken);
        }

        // A frozen copy: the engine keeps mutating the live history while a renewal re-saves it.
        _lease = instance.Status == TaleStatus.Running ? instance.Clone() : null;
    }

    private Task ReadBack(ITaleRepository savedThrough, TaleInstance written, LeaseWrite write, CancellationToken cancellationToken) =>
        readBack?.Verify(savedThrough, written, write, logger, cancellationToken) ?? Task.CompletedTask;
}
