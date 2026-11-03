using Microsoft.Extensions.Time.Testing;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>A <see cref="FakeTimeProvider"/> that counts the timers it created and those not yet disposed.</summary>
public sealed class TimerCountingTimeProvider(DateTimeOffset start) : FakeTimeProvider(start)
{
    private int _created;
    private int _live;

    public int TimersCreated => Volatile.Read(ref _created);

    public int LiveTimers => Volatile.Read(ref _live);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Interlocked.Increment(ref _created);
        Interlocked.Increment(ref _live);
        return new CountedTimer(base.CreateTimer(callback, state, dueTime, period), () => Interlocked.Decrement(ref _live));
    }

    private sealed class CountedTimer(ITimer inner, Action onDisposed) : ITimer
    {
        private int _disposed;

        public bool Change(TimeSpan dueTime, TimeSpan period) => inner.Change(dueTime, period);

        public void Dispose()
        {
            MarkDisposed();
            inner.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            MarkDisposed();
            return inner.DisposeAsync();
        }

        private void MarkDisposed()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                onDisposed();
            }
        }
    }
}
