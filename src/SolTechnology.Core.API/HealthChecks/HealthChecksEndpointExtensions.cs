using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SolTechnology.Core.API.HealthChecks;

/// <summary>
/// Endpoint extension that maps the ASP.NET health endpoint using
/// <see cref="HealthReportJsonFormatter"/> as the response writer. This is the <b>only</b>
/// ASP.NET-coupled piece of the health-check feature — it lives in <c>Core.Api</c> because that
/// package already depends on the ASP.NET shared framework.
/// </summary>
public static class HealthChecksEndpointExtensions
{
    /// <summary>
    /// Maps a health endpoint at <paramref name="path"/> that renders the report as JSON via
    /// <see cref="HealthReportJsonFormatter"/>. Status codes follow the framework default:
    /// <c>200</c> for <c>Healthy</c>/<c>Degraded</c>, <c>503</c> for <c>Unhealthy</c>.
    /// <para>
    /// Requires <c>AddHealthChecks()</c> to have been called. Chain the per-module checks
    /// (<c>AddSolSqlHealthCheck()</c>, <c>AddSolRedisHealthCheck()</c>, …) onto that builder.
    /// </para>
    /// <code>
    /// builder.Services.AddHealthChecks()
    ///     .AddSolSqlHealthCheck()
    ///     .AddSolRedisHealthCheck();
    ///
    /// app.MapSolHealthChecks("/health");
    /// </code>
    /// </summary>
    /// <param name="endpoints">The endpoint route builder (e.g. <c>WebApplication</c>).</param>
    /// <param name="path">The endpoint path (default <c>/health</c>).</param>
    public static IEndpointConventionBuilder MapSolHealthChecks(
        this IEndpointRouteBuilder endpoints,
        string path = "/health")
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapHealthChecks(path, new HealthCheckOptions
        {
            ResponseWriter = WriteJsonResponse
        });
    }

    private static async Task WriteJsonResponse(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(HealthReportJsonFormatter.Format(report));
    }
}

