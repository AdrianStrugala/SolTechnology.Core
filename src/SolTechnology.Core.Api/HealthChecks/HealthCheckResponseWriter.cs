using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SolTechnology.Core.API.HealthChecks;

/// <summary>
/// Provides JSON formatting for health check responses.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Writes a health check report as a JSON response.
    /// </summary>
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration,
            Entries = report.Entries.Select(e => new HealthCheckEntryResponse
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration,
                Description = e.Value.Description,
                Exception = e.Value.Exception?.Message,
                Data = e.Value.Data?.Count > 0 ? e.Value.Data : null
            }).ToList()
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}

/// <summary>
/// Represents a health check response.
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// The overall health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The total time taken to execute all health checks.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Individual health check results.
    /// </summary>
    public List<HealthCheckEntryResponse> Entries { get; set; } = [];
}

/// <summary>
/// Represents an individual health check entry response.
/// </summary>
public class HealthCheckEntryResponse
{
    /// <summary>
    /// The name of the health check.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The health status (Healthy, Degraded, Unhealthy).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The time taken to execute this health check.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// A description of the health check result.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Exception message if the check failed.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Additional data about the health check.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; set; }
}
