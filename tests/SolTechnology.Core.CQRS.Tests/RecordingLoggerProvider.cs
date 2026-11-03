using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.CQRS.Tests;

public sealed record RecordedLog(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, object?>> Properties);

/// <summary>Captures every log entry with its structured properties, and every scope state.</summary>
public sealed class RecordingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<RecordedLog> Logs { get; } = new();

    public ConcurrentQueue<object> Scopes { get; } = new();

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(this);

    public void Dispose()
    {
    }

    private sealed class RecordingLogger(RecordingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            provider.Scopes.Enqueue(state);
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            provider.Logs.Enqueue(new RecordedLog(logLevel, eventId, formatter(state, exception), exception, properties));
        }
    }
}
