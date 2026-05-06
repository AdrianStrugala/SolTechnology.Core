using System.ComponentModel.DataAnnotations;

namespace SolTechnology.Core.Logging;

/// <summary>
/// Tunables for <see cref="Middleware.LoggingMiddleware"/>.
/// </summary>
/// <remarks>
/// Validated on application start when registered via <c>AddCoreLogging</c>: invalid values
/// (e.g. negative <see cref="MaxLoggedJsonBodyBytes"/>) fail fast instead of silently
/// degrading observability in production.
/// </remarks>
public sealed class LoggingOptions
{
    /// <summary>Configuration section name, for binding via <c>Configuration.GetSection("Logging:Core")</c>.</summary>
    public const string SectionName = "Logging:Core";

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
    [Range(0, 16 * 1024 * 1024, ErrorMessage = "MaxLoggedJsonBodyBytes must be between 0 and 16 MB.")]
    public long MaxLoggedJsonBodyBytes { get; set; } = 64 * 1024;

    /// <summary>
    /// Request-path prefixes (case-insensitive) for which the middleware will <b>not</b> emit
    /// the <c>Started</c> / <c>Finished</c> request log entries and will not run enrichers.
    /// Correlation id propagation still happens. Use to silence health-check / liveness /
    /// metrics scrape noise (<c>/health</c>, <c>/metrics</c>, <c>/swagger</c>, ...).
    /// Defaults to an empty list.
    /// </summary>
    public IList<string> SkipPaths { get; set; } = new List<string>();
}



