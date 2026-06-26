using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.HTTP.HealthChecks;

/// <summary>
/// Registration extension for an upstream HTTP health check on the framework
/// <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class UpstreamHttpHealthCheckExtensions
{
    /// <summary>
    /// Adds a cached upstream service health check that calls <paramref name="healthPath"/> on the
    /// named <see cref="HttpClient"/> identified by <paramref name="httpClientName"/>.
    /// <para>
    /// Subclass <see cref="BaseUpstreamServiceHealthCheck{TReport}"/> with your concrete report
    /// model and override <see cref="BaseUpstreamServiceHealthCheck{TReport}.EvaluateReport"/> to
    /// map the typed report to a <see cref="HealthCheckResult"/>.
    /// </para>
    /// </summary>
    /// <typeparam name="TCheck">Concrete health-check type deriving from
    /// <see cref="BaseUpstreamServiceHealthCheck{TReport}"/>.</typeparam>
    /// <param name="builder">Framework health-checks builder.</param>
    /// <param name="name">Check name in the report.</param>
    /// <param name="failureStatus">Status on failure (default <see cref="HealthStatus.Unhealthy"/>).</param>
    /// <param name="tags">Optional tags for filtering.</param>
    public static IHealthChecksBuilder AddUpstreamHttpHealthCheck<TCheck>(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null)
        where TCheck : class, IHealthCheck
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Singleton so the check's instance result-cache survives across probe invocations.
        builder.Services.AddSingleton<TCheck>();

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => sp.GetRequiredService<TCheck>(),
            failureStatus,
            tags));
    }
}

