using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache;

/// <summary>
/// Lets a Redis consumer skip Redis at once while the shared connection is down, instead of waiting for the
/// StackExchange.Redis timeouts. The first skip of an operation logs a Warning; further skips of the same operation
/// within <see cref="WarningInterval"/> log at Debug. StackExchange.Redis reconnects the connection by itself.
/// </summary>
internal sealed class RedisConnectionGate(IConnectionMultiplexer redis, ILogger logger, TimeProvider? timeProvider = null)
{
    internal static readonly TimeSpan WarningInterval = TimeSpan.FromSeconds(30);

    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastWarnings = new();

    public bool IsClosed(string operation, string key)
    {
        if (redis.IsConnected)
        {
            return false;
        }

        if (IsWarningDue(operation))
        {
            logger.LogWarning("Redis not connected; skipping [{Operation}] for [{Key}]", operation, key);
        }
        else
        {
            logger.LogDebug("Redis not connected; skipping [{Operation}] for [{Key}]", operation, key);
        }

        return true;
    }

    private bool IsWarningDue(string operation)
    {
        var now = _time.GetUtcNow();
        if (!_lastWarnings.TryGetValue(operation, out var last))
        {
            return _lastWarnings.TryAdd(operation, now);
        }

        return now - last >= WarningInterval && _lastWarnings.TryUpdate(operation, now, last);
    }
}
