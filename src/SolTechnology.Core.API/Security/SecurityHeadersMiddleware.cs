using Microsoft.AspNetCore.Http;

namespace SolTechnology.Core.API.Security;

/// <summary>
/// Middleware that stamps a baseline set of security headers on every outbound response.
/// Headers are only added when not already present — an upstream middleware or host that sets its
/// own <c>Content-Security-Policy</c> is never clobbered.
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Register a callback so headers are written even on error responses produced
        // by the ProblemDetails pipeline (which may short-circuit after this middleware).
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            var csp = IsRelaxedPath(context.Request.Path)
                ? options.RelaxedContentSecurityPolicy
                : options.ContentSecurityPolicy;

            headers.TryAdd("Content-Security-Policy", csp);
            headers.TryAdd("X-Content-Type-Options", options.ContentTypeOptions);
            headers.TryAdd("Referrer-Policy", options.ReferrerPolicy);

            return Task.CompletedTask;
        });

        await next(context);
    }

    private bool IsRelaxedPath(PathString requestPath)
    {
        foreach (var prefix in options.RelaxedPathPrefixes)
        {
            if (requestPath.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

