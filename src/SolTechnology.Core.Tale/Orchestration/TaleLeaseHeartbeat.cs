using Microsoft.Extensions.Logging;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Renews the Running lease of a persisted run every third of <see cref="TaleOptions.RunningLeaseTimeout"/>
/// while its steps execute, so a long chapter is not taken over while its host is alive. When a
/// renewal finds that another run took the tale over, it cancels <see cref="Token"/>.
/// </summary>
internal sealed class TaleLeaseHeartbeat(
    TaleStatePersistence persistence,
    ILogger logger,
    Auid taleId,
    CancellationToken runToken) : IAsyncDisposable
{
    private readonly CancellationTokenSource _ownership = CancellationTokenSource.CreateLinkedTokenSource(runToken);
    private readonly Lock _lock = new();
    private ITimer? _timer;
    private Task _renewal = Task.CompletedTask;
    private bool _renewing;
    private bool _beatPending;
    private bool _stopped;

    public TaleLeaseHeartbeat Start(TimeProvider timeProvider, TimeSpan runningLeaseTimeout)
    {
        var interval = runningLeaseTimeout / 3;
        _timer = timeProvider.CreateTimer(_ => Beat(), null, interval, interval);
        return this;
    }

    /// <summary>The run's token, also cancelled once the run has lost the tale to another run.</summary>
    public CancellationToken Token => _ownership.Token;

    public async ValueTask DisposeAsync()
    {
        Task renewal;
        lock (_lock)
        {
            _stopped = true;
            renewal = _renewal;
        }

        if (_timer != null)
        {
            await _timer.DisposeAsync();
        }

        await renewal;
        _ownership.Dispose();
    }

    private void Beat()
    {
        lock (_lock)
        {
            if (_stopped)
            {
                return;
            }

            // A beat during a slow renewal renews once more right after it, never in parallel.
            _beatPending = _renewing;
            if (!_renewing)
            {
                _renewing = true;
                _renewal = Task.Run(RenewWhileBeating);
            }
        }
    }

    private async Task RenewWhileBeating()
    {
        do
        {
            await Renew();
        }
        while (TakePendingBeat());
    }

    private bool TakePendingBeat()
    {
        lock (_lock)
        {
            var pending = _beatPending && !_stopped;
            _beatPending = false;
            _renewing = pending;
            return pending;
        }
    }

    private async Task Renew()
    {
        try
        {
            var renewed = await persistence.Renew(runToken);
            if (renewed.Error is ConflictError)
            {
                logger.LogWarning("Tale [{TaleId}] was taken over by another run; stopping this run", taleId);
                await _ownership.CancelAsync();
            }
            else if (renewed.IsFailure)
            {
                logger.LogWarning("Tale [{TaleId}] could not renew its Running lease: [{Message}]",
                    taleId, renewed.Error!.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tale [{TaleId}] could not renew its Running lease", taleId);
        }
    }
}
