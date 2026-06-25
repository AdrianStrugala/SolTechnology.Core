using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SolTechnology.Core.SQL.Connections;

namespace SolTechnology.Core.SQL.HealthChecks;

/// <summary>
/// Connectivity health check for the configured SQL Server: opens a fresh connection and runs
/// <c>SELECT 1</c>. Reachable → <see cref="HealthStatus.Healthy"/>, unreachable → the configured
/// failure status. Caller-cancellation is rethrown (a cancelled probe is not an unhealthy
/// dependency); a per-call timeout guards against a hung server.
/// </summary>
internal sealed class SqlHealthCheck(ISQLConnectionFactory connectionFactory, TimeSpan timeout) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Deliberately bypass the factory's open-with-retry policy: a health probe must be
            // fast and honest, not wait through a 3/9/27s retry ladder against a dead server.
            await using var connection = new SqlConnection(connectionFactory.GetConnectionString());
            await connection.OpenAsync(linked.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(linked.Token);

            return HealthCheckResult.Healthy("SQL reachable");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled the probe — not an unhealthy dependency. Rethrow.
            throw;
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "SQL unreachable", ex);
        }
    }
}

