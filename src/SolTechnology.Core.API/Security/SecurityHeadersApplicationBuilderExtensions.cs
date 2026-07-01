using Microsoft.AspNetCore.Builder;

namespace SolTechnology.Core.API.Security;

/// <summary>
/// Extension methods to register the security-headers middleware in the ASP.NET Core pipeline.
/// </summary>
public static class SecurityHeadersApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="SecurityHeadersMiddleware"/> to the request pipeline. Every response
    /// receives strict <c>Content-Security-Policy</c>, <c>X-Content-Type-Options</c>, and
    /// <c>Referrer-Policy</c> headers. Pre-existing headers set upstream are not overwritten.
    /// <para>
    /// By default Swagger/Redoc paths receive a relaxed CSP so their UIs can function.
    /// Configure via the <paramref name="configure"/> delegate to adjust behaviour.
    /// </para>
    /// <code>
    /// app.UseSolSecurityHeaders();
    ///
    /// // Or with customisation:
    /// app.UseSolSecurityHeaders(o =&gt;
    /// {
    ///     o.ReferrerPolicy = "strict-origin-when-cross-origin";
    ///     o.RelaxedPathPrefixes.Add("/my-docs");
    /// });
    /// </code>
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">Optional delegate to customise the security header values.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseSolSecurityHeaders(
        this IApplicationBuilder app,
        Action<SecurityHeadersOptions>? configure = null)
    {
        var options = new SecurityHeadersOptions();
        configure?.Invoke(options);

        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }
}

