namespace SolTechnology.Core.API.Security;

/// <summary>
/// Options for the security-headers middleware registered via
/// <see cref="SecurityHeadersApplicationBuilderExtensions.UseSecurityHeaders"/>.
/// </summary>
public sealed class SecurityHeadersOptions
{
    /// <summary>
    /// The <c>Content-Security-Policy</c> header value applied to responses whose path does
    /// <b>not</b> match any entry in <see cref="RelaxedPathPrefixes"/>.
    /// Default: <c>default-src 'none'; frame-ancestors 'none'</c> (strictest possible — ideal for
    /// JSON APIs that serve no HTML).
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'none'; frame-ancestors 'none'";

    /// <summary>
    /// The <c>Content-Security-Policy</c> header value applied to responses whose path matches
    /// an entry in <see cref="RelaxedPathPrefixes"/>. Swagger UI and Redoc require inline styles
    /// and scripts to function.
    /// </summary>
    public string RelaxedContentSecurityPolicy { get; set; } =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'";

    /// <summary>
    /// Path prefixes where the relaxed CSP applies instead of the strict default.
    /// Default: <c>["/swagger", "/docs"]</c> (covers Swashbuckle and Redoc routes).
    /// Set to an empty list to enforce the strict policy everywhere.
    /// </summary>
    public List<string> RelaxedPathPrefixes { get; set; } = ["/swagger", "/docs"];

    /// <summary>
    /// The <c>X-Content-Type-Options</c> header value. Default: <c>nosniff</c>.
    /// </summary>
    public string ContentTypeOptions { get; set; } = "nosniff";

    /// <summary>
    /// The <c>Referrer-Policy</c> header value. Default: <c>no-referrer</c> (strictest — no URL
    /// leaks to third parties).
    /// </summary>
    public string ReferrerPolicy { get; set; } = "no-referrer";
}

