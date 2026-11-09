using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.MessageBus.Tests;

internal sealed record CapturedLog(string Category, LogLevel Level, string? Template, Exception? Exception);

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLog> _entries = new();

    public IReadOnlyCollection<CapturedLog> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<CapturedLog> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var template = (state as IEnumerable<KeyValuePair<string, object?>>)?
                .FirstOrDefault(pair => pair.Key == "{OriginalFormat}").Value as string;
            entries.Enqueue(new CapturedLog(category, logLevel, template, exception));
        }
    }
}
