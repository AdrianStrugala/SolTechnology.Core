using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace SolTechnology.Core.Logging.Middleware;

/// <summary>
/// Middleware that provides comprehensive request logging with:
/// - Correlation ID extraction/generation and propagation
/// - Custom identifier extraction for domain-specific logging
/// - Request timing and status code logging
/// </summary>
public class LoggingMiddleware(RequestDelegate next, IOptions<LoggingMiddlewareOptions> options)
{
    private readonly LoggingMiddlewareOptions _options = options.Value;

    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    /// <summary>
    /// Gets the current correlation ID for the executing async context.
    /// </summary>
    public static string? Current => CurrentCorrelationId.Value;

    public async Task InvokeAsync(HttpContext context, ILogger<LoggingMiddleware> logger)
    {
        var stopwatch = new AsyncStopwatch();

        // Extract or generate correlation ID
        var correlationId = GetOrCreateCorrelationId(context);
        CurrentCorrelationId.Value = correlationId;

        // Add correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(_options.CorrelationIdHeader))
            {
                context.Response.Headers[_options.CorrelationIdHeader] = correlationId;
            }
            return Task.CompletedTask;
        });

        // Build scope identifiers
        var identifiers = new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId
        };

        // Extract configured identifiers
        foreach (var identifier in _options.Identifiers)
        {
            var value = ExtractIdentifier(context, identifier);
            if (value != null)
            {
                identifiers[identifier] = value;
            }
        }

        using (logger.BeginScope(identifiers))
        {
            logger.LogInformation("Started request: [{RequestMethod} {RequestPath}]",
                context.Request.Method, context.Request.Path);

            try
            {
                await next(context);
            }
            finally
            {
                logger.LogInformation("Finished request in [{ElapsedMilliseconds}] ms with status code [{StatusCode}]",
                    stopwatch.Elapsed.TotalMilliseconds, context.Response.StatusCode);

                CurrentCorrelationId.Value = null;
            }
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out StringValues existingCorrelationId) &&
            !string.IsNullOrEmpty(existingCorrelationId))
        {
            return existingCorrelationId.ToString();
        }

        return Auid.New("COR").ToString();
    }

    private static string? ExtractIdentifier(HttpContext context, string identifierName)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Try to extract from route: /{identifierName}/{value} or /{identifierName}s/{value}
        // Pattern matches: /city/Warsaw, /cities/Warsaw, /trip/123, /trips/123
        var routePattern = $@"/{Regex.Escape(identifierName)}s?/([^/]+)";
        var routeMatch = Regex.Match(path, routePattern, RegexOptions.IgnoreCase);
        if (routeMatch.Success)
        {
            return Uri.UnescapeDataString(routeMatch.Groups[1].Value);
        }

        // Try to extract from query parameter
        if (context.Request.Query.TryGetValue(identifierName, out var queryValue) &&
            !string.IsNullOrEmpty(queryValue))
        {
            return queryValue.ToString();
        }

        return null;
    }
}

/// <summary>
/// Extension methods for adding LoggingMiddleware to the application pipeline.
/// </summary>
public static class LoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds the LoggingMiddleware to the application pipeline with default options.
    /// </summary>
    public static IApplicationBuilder UseLoggingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LoggingMiddleware>();
    }

    /// <summary>
    /// Adds the LoggingMiddleware to the application pipeline with custom options.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="configure">Action to configure logging middleware options.</param>
    /// <example>
    /// <code>
    /// app.UseLoggingMiddleware(options =>
    /// {
    ///     options.CorrelationIdHeader = "X-Request-Id";
    ///     options.Identifiers = ["tripId", "cityId", "userId"];
    /// });
    /// </code>
    /// </example>
    public static IApplicationBuilder UseLoggingMiddleware(
        this IApplicationBuilder builder,
        Action<LoggingMiddlewareOptions> configure)
    {
        var options = new LoggingMiddlewareOptions();
        configure(options);

        return builder.UseMiddleware<LoggingMiddleware>(Options.Create(options));
    }
}
