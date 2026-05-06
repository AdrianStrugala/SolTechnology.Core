namespace SolTechnology.Core.Logging;

/// <summary>
/// Tunables for <see cref="Middleware.LoggingMiddleware"/>.
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>
    /// When <c>true</c>, the middleware logs a warning when an inbound
    /// <c>X-Correlation-Id</c> header fails validation (too long, etc.).
    /// </summary>
    public bool LogClientCorrelationParseErrors { get; set; } = true;

    /// <summary>
    /// Maximum JSON body size (in bytes) the middleware will buffer and parse for
    /// <c>LogDetail(..., source: LogDetailSource.Body)</c> registrations.
    /// Larger requests are skipped (no scope property added). Default: 64 KB.
    /// </summary>
    public long MaxLoggedJsonBodyBytes { get; set; } = 64 * 1024;
}



