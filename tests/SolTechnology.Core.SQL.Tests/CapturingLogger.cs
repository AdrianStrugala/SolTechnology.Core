using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.SQL.Tests;

internal sealed record CapturedLog(LogLevel Level, string? Template, IReadOnlyDictionary<string, object?> Properties);

internal sealed class CapturingLogger : ILogger
{
    public List<CapturedLog> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var properties = (state as IEnumerable<KeyValuePair<string, object?>>)?.ToDictionary(pair => pair.Key, pair => pair.Value)
            ?? new Dictionary<string, object?>();
        lock (Entries)
        {
            Entries.Add(new CapturedLog(logLevel, properties.GetValueOrDefault("{OriginalFormat}") as string, properties));
        }
    }
}
