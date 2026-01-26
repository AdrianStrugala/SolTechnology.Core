namespace SolTechnology.Core.Logging.Middleware;

/// <summary>
/// Configuration options for the LoggingMiddleware.
/// </summary>
public class LoggingMiddlewareOptions
{
    /// <summary>
    /// The HTTP header name used for correlation ID.
    /// Default: "X-Correlation-ID"
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// List of identifier names to extract from requests.
    /// For each identifier, the middleware will try to extract value from:
    /// 1. Route path: /{identifierName}/{value} (e.g., /city/Warsaw for "city")
    /// 2. Query parameter: ?{identifierName}={value} (e.g., ?cityId=Warsaw for "cityId")
    /// </summary>
    /// <example>
    /// <code>
    /// options.Identifiers = ["tripId", "cityId", "userId"];
    /// </code>
    /// </example>
    public string[] Identifiers { get; set; } = [];
}
