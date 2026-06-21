using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Npgsql;
using Respawn;
using SolTechnology.Core.Testing.Containers;
using Testcontainers.PostgreSql;

namespace SolTechnology.Core.SQL.Testing.Engines;

/// <summary>
/// PostgreSQL engine. Uses the dedicated
/// <see cref="PostgreSqlBuilder"/> (Testcontainers.PostgreSql is safe — only MSSQL had the
/// <c>sqlcmd</c> wait pitfall) with a log-based readiness probe.
/// </summary>
internal sealed class PostgresEngine(string? image, string databaseName, string containerName) : IDatabaseEngine
{
    private const string DefaultImage = "postgres:16.9-alpine";
    private const string AdminDatabase = "postgres";

    private PostgreSqlContainer? _container;


    public string ServerConnectionString => DatabaseConnectionString(AdminDatabase);

    public string DatabaseConnectionString(string database) =>
        new NpgsqlConnectionStringBuilder(RequireContainer().GetConnectionString())
        {
            Database = database
        }.ToString();

    public Task StartAsync(INetwork? network, string? networkAlias, CancellationToken ct)
    {
        var builder = new PostgreSqlBuilder(image ?? DefaultImage)
            .WithDatabase(databaseName)
            .WithCleanUp(!TestContainersContext.ReuseContainers)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .AddCustomWaitStrategy(new PostgresReadinessWaitStrategy()));

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

    public DbConnection OpenConnection(string connectionString) => new NpgsqlConnection(connectionString);

    // The Postgres container creates the application database via PostgreSqlBuilder.WithDatabase.
    public Task EnsureDatabaseAsync(string databaseName, CancellationToken ct) => Task.CompletedTask;

    public RespawnerOptions BuildResetOptions() => new()
    {
        DbAdapter = DbAdapter.Postgres,
        SchemasToExclude = ["pg_catalog", "information_schema"]
    };

    public async ValueTask DisposeAsync()
    {
        if (_container is not null && !TestContainersContext.ReuseContainers)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    private PostgreSqlContainer RequireContainer() =>
        _container ?? throw new InvalidOperationException("Container not started. Call StartAsync first.");
}


