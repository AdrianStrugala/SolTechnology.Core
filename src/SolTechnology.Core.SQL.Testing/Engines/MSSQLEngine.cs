using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Respawn;
using SolTechnology.Core.Testing.Containers;

namespace SolTechnology.Core.SQL.Testing.Engines;

/// <summary>
/// SQL Server engine. Uses a generic <see cref="ContainerBuilder"/> instead of <c>MsSqlBuilder</c>
/// from <c>Testcontainers.MsSql</c>: the latter forces a wait that shells out to <c>sqlcmd</c> inside
/// the container — missing from <c>azure-sql-edge</c>, whose failure cascades into a confusing
/// <c>Could not find resource 'MsSqlContainer'</c> once the container is auto-removed. Wait is
/// host-side only (log + authenticated login probe).
/// </summary>
internal sealed class MSSQLEngine(string? image, string containerName) : IDatabaseEngine
{
    private const string DefaultImage = "mcr.microsoft.com/mssql/server:2022-latest";
    private const string SaPassword = "Strong_p@ssw0rd!";
    private const int InternalPort = 1433;

    private IContainer? _container;

    private IContainer RequireContainer() =>
        _container ?? throw new InvalidOperationException("Container not started. Call StartAsync first.");

    public string ServerConnectionString
    {
        get
        {
            var host = RequireContainer().Hostname;
            var port = RequireContainer().GetMappedPublicPort(InternalPort);
            return new SqlConnectionStringBuilder
            {
                DataSource = $"{host},{port}",
                UserID = "sa",
                Password = SaPassword,
                TrustServerCertificate = true,
                Encrypt = false,
                ConnectTimeout = 30
            }.ToString();
        }
    }

    public string DatabaseConnectionString(string databaseName) =>
        new SqlConnectionStringBuilder(ServerConnectionString) { InitialCatalog = databaseName }.ToString();

    public Task StartAsync(INetwork? network, string? networkAlias, CancellationToken ct)
    {
        var builder = new ContainerBuilder(image ?? DefaultImage)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
            .WithEnvironment("MSSQL_PID", "Developer")
            .WithPortBinding(InternalPort, assignRandomHostPort: true)
            .WithCleanUp(!TestContainersContext.ReuseContainers)
            // Host-side only: `nc`/`sqlcmd` are not present in some SQL images.
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("SQL Server is now ready for client connections")
                .AddCustomWaitStrategy(new SQLServerLoginWaitStrategy(() => ServerConnectionString)));

        if (TestContainersContext.ReuseContainers)
        {
            builder = builder.WithName(containerName).WithReuse(true);
        }

        if (network is not null)
        {
            builder = builder.WithNetwork(network);
            if (!string.IsNullOrEmpty(networkAlias))
            {
                builder = builder.WithNetworkAliases(networkAlias);
            }
        }

        _container = builder.Build();
        return _container.StartAsync(ct);
    }

    public DbConnection OpenConnection(string connectionString) => new SqlConnection(connectionString);

    public async Task EnsureDatabaseAsync(string databaseName, CancellationToken ct)
    {
        var masterCs = new SqlConnectionStringBuilder(ServerConnectionString) { InitialCatalog = "master" }.ToString();
        await using var connection = new SqlConnection(masterCs);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        // QUOTENAME guards the identifier; build the statement into a variable first because EXEC()
        // does not allow a function call (QUOTENAME) inside its string-concatenation argument.
        command.CommandText =
            "IF DB_ID(@name) IS NULL BEGIN DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@name); EXEC(@sql); END";
        command.Parameters.AddWithValue("@name", databaseName);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public RespawnerOptions BuildResetOptions() => new()
    {
        DbAdapter = DbAdapter.SqlServer,
        SchemasToExclude = ["sys", "INFORMATION_SCHEMA"]
    };

    public async ValueTask DisposeAsync()
    {
        if (_container is not null && !TestContainersContext.ReuseContainers)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}



