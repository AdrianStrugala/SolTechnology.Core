using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache.HealthChecks;

/// <summary>
/// Registration extension for the Redis connectivity health check on the framework
/// <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class RedisHealthCheckExtensions
{
    /// <summary>
    /// Adds a <c>PING</c> connectivity check for the distributed cache. Requires
    /// <see cref="IConnectionMultiplexer"/> to be registered (via <c>AddDistributedCache(...)</c>).
    /// </summary>
    /// <param name="builder">The framework health-checks builder (from <c>AddHealthChecks()</c>).</param>
    /// <param name="name">Check name in the report (default <c>redis</c>).</param>
    /// <param name="timeout">Per-call probe timeout (default 5s) — guards against a hung server.</param>
    /// <param name="failureStatus">Status reported on failure (default <see cref="HealthStatus.Unhealthy"/>).</param>
    /// <param name="tags">Optional tags for endpoint filtering (e.g. <c>"ready"</c>).</param>
    public static IHealthChecksBuilder AddRedisHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "redis",
        TimeSpan? timeout = null,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var probeTimeout = timeout ?? TimeSpan.FromSeconds(5);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new RedisHealthCheck(sp.GetRequiredService<IConnectionMultiplexer>(), probeTimeout),
            failureStatus,
            tags));
    }
}

