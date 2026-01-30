using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SolTechnology.Core.API.HealthChecks;

namespace DreamTravel.Api.HealthChecks;

/// <summary>
/// Health check for SQL Server database connectivity.
/// </summary>
public class DatabaseHealthCheck(IConfiguration configuration, ILogger<DatabaseHealthCheck> logger)
    : CachedHealthCheck(TimeSpan.FromSeconds(30))
{
    private readonly string _connectionString = configuration.GetSection("Sql:ConnectionString").Value
                                                ?? throw new ArgumentNullException(nameof(configuration), "SQL connection string is not configured");

    protected override async Task<HealthCheckResult> ExecuteHealthCheckAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            logger.LogDebug("Database health check passed");

            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Database health check failed");

            return HealthCheckResult.Unhealthy(
                "Database connection failed",
                ex,
                new Dictionary<string, object>
                {
                    ["ErrorCode"] = ex.Number,
                    ["ErrorMessage"] = ex.Message
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database health check failed with unexpected error");

            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
