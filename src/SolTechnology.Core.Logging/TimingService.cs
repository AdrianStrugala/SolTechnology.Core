using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace SolTechnology.Core.Logging;

/// <summary>
/// Default implementation of <see cref="ITimingService"/>. Uses <see cref="TimeProvider"/> for
/// testability (no direct <c>Stopwatch.GetTimestamp()</c>). Storage is instance-scoped (register
/// as Scoped in DI so each request gets its own instance).
/// </summary>
internal sealed class TimingService(TimeProvider timeProvider) : ITimingService
{
    private readonly ConcurrentDictionary<string, long> _timings = new(StringComparer.Ordinal);

    public IDisposable StartContext(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new TimingHandle(this, name, timeProvider.GetTimestamp());
    }

    public IDictionary<string, long> GetTimings()
        => _timings.IsEmpty
            ? ReadOnlyDictionary<string, long>.Empty
            : new ReadOnlyDictionary<string, long>(new Dictionary<string, long>(_timings.ToArray(), StringComparer.Ordinal));

    public void Reset() => _timings.Clear();

    private void StopContext(string name, long startTimestamp)
    {
        var elapsed = timeProvider.GetElapsedTime(startTimestamp);
        _timings.AddOrUpdate(
            name,
            _ => (long)elapsed.TotalMilliseconds,
            (_, existing) => existing + (long)elapsed.TotalMilliseconds);
    }

    private sealed class TimingHandle(TimingService owner, string name, long startTimestamp) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.StopContext(name, startTimestamp);
            }
        }
    }
}

