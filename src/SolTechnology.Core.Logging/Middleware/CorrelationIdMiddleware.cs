using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace SolTechnology.Core.Logging.Middleware;

/// <summary>
/// Middleware that extracts or generates a correlation ID for request tracing.
/// The correlation ID is extracted from the X-Correlation-ID header if present,
/// otherwise a new one is generated. The ID is added to both the log scope and response headers.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>
    /// The header name used for correlation ID.
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-ID";

    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    /// <summary>
    /// Gets the current correlation ID for the executing async context.
    /// </summary>
    public static string? Current => CurrentCorrelationId.Value;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Store in AsyncLocal for access anywhere in the request pipeline
        CurrentCorrelationId.Value = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
            }
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }

        CurrentCorrelationId.Value = null;
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out StringValues existingCorrelationId) &&
            !string.IsNullOrEmpty(existingCorrelationId))
        {
            return existingCorrelationId.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension methods for adding CorrelationIdMiddleware to the application pipeline.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the CorrelationIdMiddleware to the application pipeline.
    /// This should be added early in the pipeline to ensure all subsequent middleware has access to the correlation ID.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
