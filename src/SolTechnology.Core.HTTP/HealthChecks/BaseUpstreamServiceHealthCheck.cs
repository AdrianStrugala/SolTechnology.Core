using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.HTTP.HealthChecks;

/// <summary>
/// Options for configuring <see cref="BaseUpstreamServiceHealthCheck{TReport}"/>.
/// </summary>
public sealed class UpstreamHealthCheckOptions
{
    /// <summary>
    /// How long a successful probe result is cached before re-hitting the upstream.
    /// Default 30 seconds.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Per-call timeout for the upstream HTTP request. Independent of the health-check probe
    /// deadline. Default 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Base class for a cached upstream service health check that calls a downstream
/// <c>/health</c> endpoint, deserialises a typed <typeparamref name="TReport"/>, and maps
/// the exception taxonomy:
/// <list type="bullet">
///   <item>Connection failure → <c>Unhealthy</c></item>
///   <item>Timeout → <c>Unhealthy</c></item>
///   <item>Caller-cancellation → <b>rethrow</b> (not <c>Unhealthy</c>)</item>
///   <item>Bad payload (deserialisation failure) → <c>Degraded</c></item>
///   <item>2xx with valid report → mapped via <see cref="EvaluateReport"/></item>
/// </list>
/// Lives in <c>Core.HTTP</c> because it probes a downstream over <see cref="HttpClient"/>.
/// </summary>
/// <typeparam name="TReport">Typed report model deserialised from the upstream JSON body.</typeparam>
public abstract class BaseUpstreamServiceHealthCheck<TReport>(
    HttpClient httpClient,
    string healthPath,
    UpstreamHealthCheckOptions options,
    ILogger logger,
    TimeProvider? timeProvider = null) : IHealthCheck
    where TReport : class
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    // Instance cache: the check is registered as a singleton (see UpstreamHttpHealthCheckExtensions)
    // so a fresh result is cached across probe invocations without leaking across other
    // registrations or test instances.
    private readonly ConcurrentDictionary<string, (HealthCheckResult Result, DateTimeOffset CachedAt)> _cache = new();

    /// <summary>
    /// Override to map the deserialised <typeparamref name="TReport"/> to a
    /// <see cref="HealthCheckResult"/>. Called only when the upstream returned 2xx with a
    /// valid body.
    /// </summary>
    protected abstract HealthCheckResult EvaluateReport(TReport report);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{httpClient.BaseAddress}{healthPath}";

        // Return cached if still fresh.
        if (_cache.TryGetValue(cacheKey, out var cached) &&
            _timeProvider.GetUtcNow() - cached.CachedAt < options.CacheDuration)
        {
            return cached.Result;
        }

        using var timeoutCts = new CancellationTokenSource(options.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var response = await httpClient.GetAsync(healthPath, linked.Token);
            response.EnsureSuccessStatusCode();

            var report = await response.Content.ReadFromJsonAsync<TReport>(linked.Token);
            if (report is null)
            {
                var degraded = HealthCheckResult.Degraded("Upstream returned null report");
                _cache[cacheKey] = (degraded, _timeProvider.GetUtcNow());
                return degraded;
            }

            var result = EvaluateReport(report);
            _cache[cacheKey] = (result, _timeProvider.GetUtcNow());
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled — not an unhealthy dependency. Rethrow.
            throw;
        }
        catch (OperationCanceledException)
        {
            // Our per-call timeout fired — upstream is too slow.
            var unhealthy = new HealthCheckResult(
                context.Registration.FailureStatus, "Upstream timed out");
            _cache[cacheKey] = (unhealthy, _timeProvider.GetUtcNow());
            return unhealthy;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Upstream health check failed for {HealthPath}", cacheKey);
            var unhealthy = new HealthCheckResult(
                context.Registration.FailureStatus, "Upstream connection failure", ex);
            _cache[cacheKey] = (unhealthy, _timeProvider.GetUtcNow());
            return unhealthy;
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Deserialization failed — the upstream is reachable but returning garbage.
            logger.LogWarning(ex, "Bad payload from upstream {HealthPath}", cacheKey);
            var degraded = HealthCheckResult.Degraded("Bad payload from upstream", ex);
            _cache[cacheKey] = (degraded, _timeProvider.GetUtcNow());
            return degraded;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error in upstream health check {HealthPath}", cacheKey);
            var unhealthy = new HealthCheckResult(
                context.Registration.FailureStatus, "Unexpected error", ex);
            _cache[cacheKey] = (unhealthy, _timeProvider.GetUtcNow());
            return unhealthy;
        }
    }
}

