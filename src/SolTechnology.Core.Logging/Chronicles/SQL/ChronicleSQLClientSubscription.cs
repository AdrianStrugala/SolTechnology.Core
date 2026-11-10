using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Logging.Chronicles.SQL;

/// <summary>
/// Subscribes this host's <see cref="ChronicleSQLClientObserver"/> to the process-wide
/// <see cref="DiagnosticListener.AllListeners"/> on start, and unsubscribes on stop or dispose.
/// </summary>
internal sealed class ChronicleSQLClientSubscription(IChronicle chronicle, IOptions<ChronicleOptions> options) : IHostedService, IDisposable
{
    private readonly Lock _gate = new();
    private ChronicleSQLClientObserver? _observer;
    private IDisposable? _allListeners;

    internal ChronicleSQLClientObserver? Observer => _observer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_observer is null && options.Value.CaptureSQLCommands && chronicle is ChronicleRecorder recorder)
            {
                _observer = new ChronicleSQLClientObserver(recorder.Writer);
                _allListeners = DiagnosticListener.AllListeners.Subscribe(_observer);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _allListeners?.Dispose();
            _observer?.Dispose();
            (_allListeners, _observer) = (null, null);
        }
    }
}
