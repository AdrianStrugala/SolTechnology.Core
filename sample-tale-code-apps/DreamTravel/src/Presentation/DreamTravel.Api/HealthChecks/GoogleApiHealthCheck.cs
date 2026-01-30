using Microsoft.Extensions.Diagnostics.HealthChecks;
using SolTechnology.Core.API.HealthChecks;

namespace DreamTravel.Api.HealthChecks;

/// <summary>
/// Health check for Google Maps API availability.
/// </summary>
public class GoogleApiHealthCheck(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GoogleApiHealthCheck> logger)
    : CachedHealthCheck(TimeSpan.FromSeconds(30))
{
    protected override async Task<HealthCheckResult> ExecuteHealthCheckAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = configuration.GetSection("Google:Key").Value;
            if (string.IsNullOrEmpty(apiKey))
            {
                return HealthCheckResult.Degraded("Google API key is not configured");
            }

            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(
                $"https://maps.googleapis.com/maps/api/geocode/json?address=test&key={apiKey}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogDebug("Google API health check passed");
                return HealthCheckResult.Healthy("Google API is reachable");
            }

            logger.LogWarning("Google API health check returned status code [{StatusCode}]", response.StatusCode);

            return HealthCheckResult.Degraded(
                $"Google API returned status code {response.StatusCode}",
                data: new Dictionary<string, object>
                {
                    ["StatusCode"] = (int)response.StatusCode
                });
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("Google API health check timed out");
            return HealthCheckResult.Degraded("Google API request timed out");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Google API health check failed");
            return HealthCheckResult.Unhealthy("Google API is unreachable", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google API health check failed with unexpected error");
            return HealthCheckResult.Unhealthy("Google API health check failed", ex);
        }
    }
}
