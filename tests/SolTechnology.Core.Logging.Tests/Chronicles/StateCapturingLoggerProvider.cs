using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

/// <summary>Keeps the original structured state of every entry; <c>FakeLogger</c> stringifies state values.</summary>
internal sealed class StateCapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<(LogLevel Level, EventId EventId, KeyValuePair<string, object?>[] State)> Entries { get; } = new();

    public bool Capture { get; init; } = true;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(StateCapturingLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (owner.Capture && state is IReadOnlyList<KeyValuePair<string, object?>> pairs)
            {
                owner.Entries.Enqueue((logLevel, eventId, [.. pairs]));
            }
        }
    }
}
