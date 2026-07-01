using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SolTechnology.Core.SQL.Connections;

namespace SolTechnology.Core.SQL.HealthChecks;

/// <summary>
/// Registration extension for the SQL connectivity health check on the framework
/// <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class SqlHealthCheckExtensions
{
    /// <summary>
    /// Adds a <c>SELECT 1</c> connectivity check for the configured SQL Server. Requires
    /// <see cref="ISQLConnectionFactory"/> to be registered (via <c>AddSql(...)</c>).
    /// </summary>
    /// <param name="builder">The framework health-checks builder (from <c>AddHealthChecks()</c>).</param>
    /// <param name="name">Check name in the report (default <c>sql</c>).</param>
    /// <param name="timeout">Per-call probe timeout (default 5s) — guards against a hung server.</param>
    /// <param name="failureStatus">Status reported on failure (default <see cref="HealthStatus.Unhealthy"/>).</param>
    /// <param name="tags">Optional tags for endpoint filtering (e.g. <c>"ready"</c>).</param>
    public static IHealthChecksBuilder AddSolSqlHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "sql",
        TimeSpan? timeout = null,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var probeTimeout = timeout ?? TimeSpan.FromSeconds(5);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new SqlHealthCheck(sp.GetRequiredService<ISQLConnectionFactory>(), probeTimeout),
            failureStatus,
            tags));
    }
}

