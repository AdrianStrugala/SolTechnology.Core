using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SolTechnology.Core.API.HealthChecks;

/// <summary>
/// Base class for health checks that caches results to prevent overwhelming upstream services.
/// Use this for external service health checks (databases, APIs, etc.) where frequent checks could cause issues.
/// </summary>
public abstract class CachedHealthCheck : IHealthCheck
{
    private readonly TimeSpan _cacheDuration;
    private HealthCheckResult? _cachedResult;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Creates a new cached health check with the specified cache duration.
    /// </summary>
    /// <param name="cacheDuration">How long to cache health check results. Defaults to 30 seconds.</param>
    protected CachedHealthCheck(TimeSpan? cacheDuration = null)
    {
        _cacheDuration = cacheDuration ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Performs the health check, returning cached result if available.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_cachedResult.HasValue && DateTime.UtcNow - _lastCheckTime < _cacheDuration)
        {
            return _cachedResult.Value;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring the lock
            if (_cachedResult.HasValue && DateTime.UtcNow - _lastCheckTime < _cacheDuration)
            {
                return _cachedResult.Value;
            }

            var result = await ExecuteHealthCheckAsync(context, cancellationToken);
            _cachedResult = result;
            _lastCheckTime = DateTime.UtcNow;

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Override this method to implement the actual health check logic.
    /// </summary>
    protected abstract Task<HealthCheckResult> ExecuteHealthCheckAsync(HealthCheckContext context, CancellationToken cancellationToken);
}
