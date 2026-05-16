namespace SolTechnology.Core.Logging;

/// <summary>
/// Curated lists of well-known constants used by <see cref="LoggingOptions"/>.
/// Exposed so consumers can opt into the library's defaults explicitly rather
/// than copy-pasting strings out of the docs.
/// </summary>
public static class LoggingDefaults
{
    /// <summary>
    /// Request-path prefixes considered "infrastructure noise" (health probes,
    /// metrics scrapes, swagger). Pass to <see cref="LoggingOptions.SkipPaths"/>
    /// to silence the request envelope logs for these endpoints — correlation
    /// is still propagated.
    /// </summary>
    public static readonly IReadOnlyList<string> InfrastructurePaths = new[]
    {
        "/health",
        "/healthz",
        "/ready",
        "/alive",
        "/live",
        "/metrics",
        "/swagger",
    };

    /// <summary>
    /// Header names whose values must never appear verbatim in logs because
    /// they typically carry credentials, tokens, or session identifiers.
    /// Matched case-insensitively. Used by the built-in request-headers
    /// enricher when <see cref="LoggingOptions.LogRequestHeaders"/> is enabled.
    /// </summary>
    /// <remarks>
    /// The list is intentionally conservative — extend via
    /// <see cref="LoggingOptions.MaskedHeaders"/> for app-specific headers
    /// such as <c>X-Internal-Token</c>.
    /// </remarks>
    public static readonly IReadOnlyList<string> SensitiveHeaders = new[]
    {
        "Authorization",
        "Proxy-Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key",
        "X-Auth-Token",
        "X-Csrf-Token",
        "X-Xsrf-Token",
        "X-Session-Id",
        "X-Session-Token",
        "X-Access-Token",
        "X-Refresh-Token",
    };

    /// <summary>
    /// Token written in place of a masked value. Distinct from a hash or
    /// truncation so it is unambiguous in dashboards.
    /// </summary>
    public const string MaskedValue = "***MASKED***";
}

