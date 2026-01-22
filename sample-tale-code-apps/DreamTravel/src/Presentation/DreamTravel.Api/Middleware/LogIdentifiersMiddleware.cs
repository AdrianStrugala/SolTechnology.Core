using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using SolTechnology.Core.Logging.Middleware;

namespace DreamTravel.Api.Middleware;

/// <summary>
/// Middleware for extracting and logging domain-specific identifiers from requests.
/// Extracts TripId, CityName, and CorrelationId and adds them to the logging scope.
/// </summary>
public partial class LogIdentifiersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LogIdentifiersMiddleware> _logger;

    [GeneratedRegex(@"/trips/([a-fA-F0-9\-]{36})", RegexOptions.Compiled)]
    private static partial Regex TripIdRouteRegex();

    [GeneratedRegex(@"/cities/([^/]+)", RegexOptions.Compiled)]
    private static partial Regex CityNameRouteRegex();

    public LogIdentifiersMiddleware(RequestDelegate next, ILogger<LogIdentifiersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var identifiers = ExtractIdentifiers(context);

        using (_logger.BeginScope(identifiers))
        {
            await _next(context);
        }
    }

    private Dictionary<string, object?> ExtractIdentifiers(HttpContext context)
    {
        var identifiers = new Dictionary<string, object?>();

        // Get or create CorrelationId (use the one from CorrelationIdMiddleware if available)
        var correlationId = CorrelationIdMiddleware.Current ?? GetCorrelationIdFromHeader(context);
        identifiers["CorrelationId"] = correlationId;

        var path = context.Request.Path.Value ?? string.Empty;

        // Extract TripId from route
        var tripIdMatch = TripIdRouteRegex().Match(path);
        if (tripIdMatch.Success && Guid.TryParse(tripIdMatch.Groups[1].Value, out var tripId))
        {
            identifiers["TripId"] = tripId;
        }

        // Extract CityName from route
        var cityNameMatch = CityNameRouteRegex().Match(path);
        if (cityNameMatch.Success)
        {
            identifiers["CityName"] = Uri.UnescapeDataString(cityNameMatch.Groups[1].Value);
        }

        // Also check query parameters for cityName
        if (context.Request.Query.TryGetValue("cityName", out var cityNameQuery) && !string.IsNullOrEmpty(cityNameQuery))
        {
            identifiers["CityName"] = cityNameQuery.ToString();
        }

        return identifiers;
    }

    private static string GetCorrelationIdFromHeader(HttpContext context)
    {
        const string correlationIdHeader = "X-Correlation-ID";

        if (context.Request.Headers.TryGetValue(correlationIdHeader, out StringValues existingCorrelationId) &&
            !string.IsNullOrEmpty(existingCorrelationId))
        {
            return existingCorrelationId.ToString();
        }

        var newCorrelationId = Guid.NewGuid().ToString("N");
        context.Response.Headers[correlationIdHeader] = newCorrelationId;

        return newCorrelationId;
    }
}

/// <summary>
/// Extension methods for adding LogIdentifiersMiddleware to the application pipeline.
/// </summary>
public static class LogIdentifiersMiddlewareExtensions
{
    /// <summary>
    /// Adds the LogIdentifiersMiddleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseLogIdentifiers(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LogIdentifiersMiddleware>();
    }
}
