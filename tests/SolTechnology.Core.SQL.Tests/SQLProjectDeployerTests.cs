using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using NUnit.Framework;
using SolTechnology.Core.SQL.SQLProject;

namespace SolTechnology.Core.SQL.Tests;

[TestFixture]
[Category("Dacpac")]
public class SQLProjectDeployerTests
{
    private string? _dacpacPath;

    [TearDown]
    public void DeleteDacpac()
    {
        if (_dacpacPath is not null)
        {
            File.Delete(_dacpacPath);
        }
    }

    [Test]
    public async Task DeployDacpac_DataLossChange_IsBlockedByDefault()
    {
        // Arrange
        var (serverConnectionString, databaseName) = await DatabaseWithNamedThing("kept");
        var dacpacPath = BuildDacpacWithoutNameColumn();

        // Act
        var deploy = () => SQLProjectDeployer.DeployDacpac(dacpacPath, serverConnectionString, databaseName, NullLogger.Instance);

        // Assert
        await deploy.Should().ThrowAsync<DacServicesException>();
        (await Query(serverConnectionString, databaseName, "SELECT Name FROM dbo.Things WHERE Id = 1")).Should().Be("kept");
    }

    [Test]
    public async Task DeployDacpac_DataLossAllowedByOptions_UpgradesInPlaceAndLogsBracketedTemplates()
    {
        // Arrange
        var (serverConnectionString, databaseName) = await DatabaseWithNamedThing("dropped");
        var dacpacPath = BuildDacpacWithoutNameColumn();
        var logger = new CapturingLogger();

        // Act
        await SQLProjectDeployer.DeployDacpac(
            dacpacPath, serverConnectionString, databaseName, logger, new DacDeployOptions { BlockOnPossibleDataLoss = false });

        // Assert
        (await Query(serverConnectionString, databaseName, "SELECT COL_LENGTH('dbo.Things', 'Name')")).Should().Be(DBNull.Value);
        (await Query(serverConnectionString, databaseName, "SELECT COUNT(*) FROM dbo.Things")).Should().Be(1);
        logger.Entries.Should().ContainSingle(entry => entry.Template == "Deploying dacpac [{DacpacPath}] to database [{DatabaseName}]")
            .Which.Properties.Should().Contain("DacpacPath", dacpacPath).And.Contain("DatabaseName", databaseName);
        logger.Entries.Should().ContainSingle(entry => entry.Template == "Database [{DatabaseName}] deployed")
            .Which.Level.Should().Be(LogLevel.Information);
        logger.Entries.Should().Contain(entry => entry.Template == "[{DacStatus}] [{DacProgress}]");
    }

    private static async Task<(string ServerConnectionString, string DatabaseName)> DatabaseWithNamedThing(string name)
    {
        var serverConnectionString = (await SQLTestEnvironment.Database()).ConnectionString;
        var databaseName = $"Deployer{Guid.NewGuid():N}";
        await Query(serverConnectionString, "master", $"CREATE DATABASE [{databaseName}]");
        await Query(serverConnectionString, databaseName, "CREATE TABLE dbo.Things (Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(50) NOT NULL)");
        await Query(serverConnectionString, databaseName, $"INSERT INTO dbo.Things (Id, Name) VALUES (1, N'{name}')");
        return (serverConnectionString, databaseName);
    }

    private string BuildDacpacWithoutNameColumn()
    {
        _dacpacPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"things-{Guid.NewGuid():N}.dacpac");
        using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects("CREATE TABLE dbo.Things (Id INT NOT NULL PRIMARY KEY)");
        DacPackageExtensions.BuildPackage(_dacpacPath, model, new PackageMetadata { Name = "Things", Version = "2.0.0" });
        return _dacpacPath;
    }

    private static async Task<object?> Query(string serverConnectionString, string databaseName, string sql)
    {
        var connectionString = new SqlConnectionStringBuilder(serverConnectionString) { InitialCatalog = databaseName }.ToString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }
}
