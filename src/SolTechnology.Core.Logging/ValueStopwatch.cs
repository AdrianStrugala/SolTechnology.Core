using System.Diagnostics;

namespace SolTechnology.Core.Logging;

/// <summary>
/// Allocation-free stopwatch built on <see cref="Stopwatch.GetTimestamp"/>.
/// Replacement for the previous <c>AsyncStopwatch</c>, which used a static
/// <see cref="System.Threading.AsyncLocal{T}"/> field and lost measurements when
/// multiple instances were created in the same async flow.
/// </summary>
public readonly struct ValueStopwatch
{
    private readonly long _startTimestamp;

    private ValueStopwatch(long startTimestamp)
    {
        _startTimestamp = startTimestamp;
    }

    /// <summary>Starts a new <see cref="ValueStopwatch"/>.</summary>
    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

    /// <summary>Elapsed time since <see cref="StartNew"/>.</summary>
    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_startTimestamp);

    /// <summary>Elapsed time in milliseconds since <see cref="StartNew"/>.</summary>
    public long ElapsedMilliseconds => (long)Elapsed.TotalMilliseconds;
}

