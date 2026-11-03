using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// In-memory repository that can hold the first N reads, after reading, until all N callers arrived,
/// so concurrent tests race on the same stored version; it can also run a hook before each save.
/// </summary>
public sealed class GatedTaleRepository(InMemoryTaleRepository inner) : ITaleRepository
{
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _gatedCallers;
    private int _arrived;

    public Func<TaleInstance, Task>? BeforeSave { get; set; }

    public void GateReads(int callers) => _gatedCallers = callers;

    public async Task<TaleInstance?> FindById(Auid taleId, CancellationToken cancellationToken)
    {
        var tale = await inner.FindById(taleId, cancellationToken);
        await WaitAtGate();
        return tale;
    }

    public async Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken)
    {
        var tale = await inner.FindByIdempotencyKey(idempotencyKey, cancellationToken);
        await WaitAtGate();
        return tale;
    }

    public Task<IReadOnlyList<TaleInstance>> List(
        TaleStatus? status = null,
        string? handlerTypeName = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
        => inner.List(status, handlerTypeName, skip, take, cancellationToken);

    public async Task<Result> Save(TaleInstance taleInstance, CancellationToken cancellationToken)
    {
        if (BeforeSave != null)
        {
            await BeforeSave(taleInstance);
        }

        return await inner.Save(taleInstance, cancellationToken);
    }

    public Task Delete(Auid taleId, CancellationToken cancellationToken) => inner.Delete(taleId, cancellationToken);

    private async Task WaitAtGate()
    {
        if (_gatedCallers == 0)
        {
            return;
        }

        var arrived = Interlocked.Increment(ref _arrived);
        if (arrived > _gatedCallers)
        {
            return;
        }

        if (arrived == _gatedCallers)
        {
            _released.TrySetResult();
        }

        await _released.Task;
    }
}
