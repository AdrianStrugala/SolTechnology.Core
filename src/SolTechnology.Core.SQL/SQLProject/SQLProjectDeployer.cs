using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;

namespace SolTechnology.Core.SQL.SQLProject;

/// <summary>
/// Deploys a <c>.dacpac</c> to SQL Server, creating the database when it does not exist and upgrading it in place
/// otherwise.
/// </summary>
public static class SQLProjectDeployer
{
    /// <summary>
    /// Deploys <paramref name="dacpacPath"/> to <paramref name="databaseName"/> on the server of
    /// <paramref name="connectionString"/>. Without <paramref name="deployOptions"/> the deployment never drops the
    /// database or objects missing from the package, and it fails with a <see cref="DacServicesException"/> when a
    /// change could lose data.
    /// </summary>
    /// <param name="dacpacPath">Path to the <c>.dacpac</c> file.</param>
    /// <param name="connectionString">Any connection string to the target server; the initial catalog is ignored.</param>
    /// <param name="databaseName">Name of the database to create or upgrade.</param>
    /// <param name="logger">Receives the deployment start, DacFx messages, progress, and completion.</param>
    /// <param name="deployOptions">DacFx options that replace the safe defaults, for example to allow data loss.</param>
    /// <param name="ct">Cancels the deployment.</param>
    /// <exception cref="FileNotFoundException">The <c>.dacpac</c> file does not exist.</exception>
    public static async Task DeployDacpac(
        string dacpacPath,
        string connectionString,
        string databaseName,
        ILogger logger,
        DacDeployOptions? deployOptions = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(logger);
        if (!File.Exists(dacpacPath))
        {
            throw new FileNotFoundException($"Dacpac not found: {dacpacPath}");
        }

        logger.LogInformation("Deploying dacpac [{DacpacPath}] to database [{DatabaseName}]", dacpacPath, databaseName);

        var masterConnectionString = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ToString();

        using var package = DacPackage.Load(dacpacPath);
        var services = new DacServices(masterConnectionString);
        services.Message += (_, e) => LogDacMessage(logger, e.Message);
        services.ProgressChanged += (_, e) => logger.LogDebug("[{DacStatus}] [{DacProgress}]", e.Status, e.Message);

        var options = deployOptions ?? new DacDeployOptions { BlockOnPossibleDataLoss = true };
        await Task.Run(() => services.Deploy(package, databaseName, upgradeExisting: true, options, ct), ct);

        logger.LogInformation("Database [{DatabaseName}] deployed", databaseName);
    }

    private static void LogDacMessage(ILogger logger, DacMessage message)
    {
        var level = message.MessageType switch
        {
            DacMessageType.Error => LogLevel.Error,
            DacMessageType.Warning => LogLevel.Warning,
            _ => LogLevel.Information
        };
        logger.Log(level, "[{DacMessage}]", message.ToString());
    }
}
