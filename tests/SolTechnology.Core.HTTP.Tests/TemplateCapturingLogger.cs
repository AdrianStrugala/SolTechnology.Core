using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.HTTP.Tests;

internal sealed record CapturedLog(LogLevel Level, string? Template, string Message, Exception? Exception);

internal sealed class TemplateCapturingLogger : ILogger, ILoggerProvider
{
    public List<CapturedLog> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var template = (state as IEnumerable<KeyValuePair<string, object?>>)?
            .FirstOrDefault(pair => pair.Key == "{OriginalFormat}").Value as string;
        lock (Entries)
        {
            Entries.Add(new CapturedLog(logLevel, template, formatter(state, exception), exception));
        }
    }

    public ILogger CreateLogger(string categoryName) => this;

    public void Dispose()
    {
    }
}
