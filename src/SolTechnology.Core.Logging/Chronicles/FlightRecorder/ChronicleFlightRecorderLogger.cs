using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Chronicles.FlightRecorder;

/// <summary>
/// Buffers an entry into the ambient open root when the recorder is enabled and the level lies
/// between <see cref="ChronicleFlightRecorderOptions.MinimumLevel"/> and Debug. Chronicle's own
/// category is never buffered, so a report can never feed the recorder.
/// </summary>
internal sealed class ChronicleFlightRecorderLogger(string category, ChronicleFlightRecorderProvider provider) : ILogger
{
    private readonly bool _isChronicleCategory = category == ChronicleLogMessages.Category;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => Accepts(logLevel) && AmbientChronicle.OpenRoot is not null;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var options = provider.Options;
        if (!Accepts(logLevel) || AmbientChronicle.OpenRoot is not { } root || !root.FlightBuffer.TryReserve(options.MaxEntries))
        {
            return;
        }

        root.FlightBuffer.Add(new ChronicleFlightRecord
        {
            OffsetMs = (long)root.Owner.TimeProvider.GetElapsedTime(root.Node.StartTimestamp).TotalMilliseconds,
            Level = logLevel,
            Category = category,
            EventId = eventId.Id,
            Message = ChronicleText.Truncate(formatter(state, exception), options.MaxMessageLength),
            ExceptionType = exception?.GetType().Name
        });
    }

    private bool Accepts(LogLevel logLevel)
        => provider.Options.Enabled && !_isChronicleCategory && logLevel >= provider.Options.MinimumLevel && logLevel <= LogLevel.Debug;
}
