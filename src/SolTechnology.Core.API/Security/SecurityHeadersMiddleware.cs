using Microsoft.AspNetCore.Http;

namespace SolTechnology.Core.API.Security;

/// <summary>
/// Middleware that stamps a baseline set of security headers on every outbound response.
/// Headers are only added when not already present — an upstream middleware or host that sets its
/// own <c>Content-Security-Policy</c> is never clobbered.
/// </summary>
internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Register a callback so headers are written even on error responses produced
        // by the ProblemDetails pipeline (which may short-circuit after this middleware).
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            var csp = IsRelaxedPath(context.Request.Path)
                ? _options.RelaxedContentSecurityPolicy
                : _options.ContentSecurityPolicy;

            headers.TryAdd("Content-Security-Policy", csp);
            headers.TryAdd("X-Content-Type-Options", _options.ContentTypeOptions);
            headers.TryAdd("Referrer-Policy", _options.ReferrerPolicy);

            return Task.CompletedTask;
        });

        await _next(context);
    }

    private bool IsRelaxedPath(PathString requestPath)
    {
        foreach (var prefix in _options.RelaxedPathPrefixes)
        {
            if (requestPath.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

