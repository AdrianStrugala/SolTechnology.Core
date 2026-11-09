using Serilog.Sinks.InMemory;

namespace SolTechnology.Core.Testing.Logging;

/// <summary>
/// Query helpers over a <see cref="InMemorySink"/> so component / integration tests can assert on the
/// application's emitted logs without taking a FluentAssertions dependency in the foundation package.
/// Wire the sink with <c>.WriteTo.Sink(InMemorySink.Instance)</c> (or a captured instance) on the
/// test host's logger.
/// </summary>
public static class InMemorySinkAssertions
{
    extension(InMemorySink sink)
    {
        /// <summary>Rendered messages of every captured log event, in order.</summary>
        public IReadOnlyList<string> Messages()
        {
            ArgumentNullException.ThrowIfNull(sink);
            return sink.LogEvents.Select(e => e.RenderMessage()).ToList();
        }

        /// <summary><see langword="true"/> if any captured event's rendered message contains <paramref name="substring"/>.</summary>
        public bool HasMessageContaining(string substring)
        {
            ArgumentNullException.ThrowIfNull(sink);
            ArgumentNullException.ThrowIfNull(substring);
            return sink.LogEvents.Any(e => e.RenderMessage().Contains(substring, StringComparison.Ordinal));
        }

        /// <summary>Number of captured events whose rendered message contains <paramref name="substring"/>.</summary>
        public int CountContaining(string substring)
        {
            ArgumentNullException.ThrowIfNull(sink);
            ArgumentNullException.ThrowIfNull(substring);
            return sink.LogEvents.Count(e => e.RenderMessage().Contains(substring, StringComparison.Ordinal));
        }

        /// <summary>Rendered messages captured at the given Serilog level name (e.g. <c>Error</c>, <c>Warning</c>).</summary>
        public IReadOnlyList<string> MessagesAtLevel(string levelName)
        {
            ArgumentNullException.ThrowIfNull(sink);
            ArgumentNullException.ThrowIfNull(levelName);
            return sink.LogEvents
                .Where(e => string.Equals(e.Level.ToString(), levelName, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.RenderMessage())
                .ToList();
        }
    }
}

