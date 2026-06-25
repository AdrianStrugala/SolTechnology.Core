using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace SolTechnology.Core.Cache.HealthChecks;

/// <summary>
/// Connectivity health check for the distributed cache (Redis): issues a <c>PING</c> via the
/// shared <see cref="IConnectionMultiplexer"/> registered by <c>AddDistributedCache</c>.
/// Reachable → <see cref="HealthStatus.Healthy"/>, unreachable → the configured failure status.
/// Caller-cancellation is rethrown; a per-call timeout guards against a hung server.
/// </summary>
internal sealed class RedisHealthCheck(IConnectionMultiplexer redis, TimeSpan timeout) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = redis.GetDatabase();

            // PingAsync has no CancellationToken overload — bound it with WaitAsync so the probe
            // honours both the per-call timeout and caller-cancellation.
            await database.PingAsync().WaitAsync(timeout, cancellationToken);

            return HealthCheckResult.Healthy("Redis reachable");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled the probe — not an unhealthy dependency. Rethrow.
            throw;
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Redis unreachable", ex);
        }
    }
}

