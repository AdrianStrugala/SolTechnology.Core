using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging;

/// <summary>
/// Thin extensions that make pushing properties into an <see cref="ILogger"/> scope a one-liner.
/// Uses <c>Dictionary&lt;string, object?&gt;</c> — the only collection type
/// <see cref="ILogger.BeginScope{TState}"/> reliably destructures as scope properties.
/// </summary>
public static class LoggerScopeExtensions
{
    /// <param name="logger">The logger whose scope receives the properties.</param>
    extension(ILogger logger)
    {
        /// <summary>Pushes a single key/value pair into the logger scope.</summary>
        public IDisposable? PushToScope(string key, object? value)
            => logger.BeginScope(new Dictionary<string, object?>(1) { [key] = value });

        /// <summary>Pushes multiple key/value pairs into the logger scope.</summary>
        public IDisposable? PushToScope(params (string Key, object? Value)[] properties)
        {
            var dict = new Dictionary<string, object?>(properties.Length);
            foreach (var (k, v) in properties)
            {
                dict[k] = v;
            }

            return logger.BeginScope(dict);
        }
    }
}
