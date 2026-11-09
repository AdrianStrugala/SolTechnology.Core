using System.Data.Common;
using SolTechnology.Core.SQL.Testing.Engines;
using SolTechnology.Core.SQL.Testing.Provisioning;

namespace SolTechnology.Core.SQL.Testing;

/// <summary>
/// Spins up a SQL Server (default) or PostgreSQL container, provisions the application schema
/// (dacpac, raw scripts or a delegate — e.g. EF migrations) and exposes a ready-to-use
/// <see cref="DatabaseConnectionString"/>. ORM-agnostic: the fixture only hands back a connection
/// string, so Dapper and EF consumers are served identically.
/// </summary>
/// <remarks>
/// The default zero-config path is unchanged from the original in-<c>Sql</c> fixture:
/// <c>new SQLFixture("Db").WithSQLProject(path)</c> deploys a dacpac to SQL Server. Opt into other
/// engines / provisioning with <see cref="UsePostgres"/>, <see cref="WithScripts"/>,
/// <see cref="WithEfMigrations"/> or <see cref="WithSchema"/>.
/// </remarks>
/// <param name="databaseName">The application database; with <c>TESTCONTAINERS_REUSE</c> the container is named <c>sol-sql-{databaseName}</c> in lower case.</param>
/// <param name="image">The engine image; defaults to <c>mssql/server:2022-latest</c>, or <c>postgres:16.9-alpine</c> after <see cref="UsePostgres"/>.</param>
public sealed partial class SQLFixture(string databaseName, string? image = null) : IAsyncDisposable
{
    private IDatabaseEngine? _engine;
    private SQLReset? _reset;

    /// <summary>The application database name.</summary>
    public string DatabaseName { get; } = databaseName ?? throw new ArgumentNullException(nameof(databaseName));

    private string ContainerName => $"sol-sql-{DatabaseName.ToLowerInvariant()}";

    /// <summary>The engine backing this fixture. Defaults to <see cref="SQLProvider.SQLServer"/>.</summary>
    public SQLProvider Provider { get; private set; } = SQLProvider.SQLServer;

    /// <summary>Server-level connection string (no specific catalog). Valid after <see cref="Initialize"/>.</summary>
    public string ConnectionString => Engine.ServerConnectionString;

    /// <summary>Connection string targeting the application database. Valid after <see cref="Initialize"/>.</summary>
    public string DatabaseConnectionString => Engine.DatabaseConnectionString(DatabaseName);

    /// <summary>
    /// Starts the engine container (or reuses the one kept by an earlier run), provisions the schema and checks that
    /// the application database accepts connections.
    /// </summary>
    /// <exception cref="InvalidOperationException">No schema provisioning was configured.</exception>
    public async Task Initialize(CancellationToken ct = default)
    {
        if (_provisioner is null)
        {
            throw new InvalidOperationException(
                "Configure schema provisioning (WithSQLProject / WithScripts / WithEfMigrations / WithSchema) before Initialize.");
        }

        _engine = Provider switch
        {
            SQLProvider.Postgres => new PostgresEngine(image, DatabaseName, ContainerName),
            _ => new MSSQLEngine(image, ContainerName)
        };

        await _engine.StartAsync(_network, _networkAlias, ct).ConfigureAwait(false);

        // MSSQL script/delegate provisioning starts from a container that has only `master`; create the
        // application catalog first. Dacpac (CreateNewDatabase) and Postgres (builder) create it themselves.
        if (!_provisioner.CreatesDatabase)
        {
            await _engine.EnsureDatabaseAsync(DatabaseName, ct).ConfigureAwait(false);
        }

        await _provisioner.ProvisionAsync(this, ct).ConfigureAwait(false);

        await using var verify = _engine.OpenConnection(DatabaseConnectionString);
        await verify.OpenAsync(ct).ConfigureAwait(false);

        _reset = new SQLReset(_engine, DatabaseConnectionString);
    }

    /// <summary>Reset the application database to empty (schema preserved) — the between-test cleanup path.</summary>
    public Task Reset(CancellationToken ct = default)
    {
        if (_reset is null)
        {
            throw new InvalidOperationException("Call Initialize before Reset.");
        }

        return _reset.ResetAsync(ct);
    }

    /// <summary>Removes the container, unless <c>TESTCONTAINERS_REUSE</c> keeps it for the next run.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_engine is not null)
        {
            await _engine.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal DbConnection CreateDatabaseConnection() => Engine.OpenConnection(DatabaseConnectionString);

    private IDatabaseEngine Engine =>
        _engine ?? throw new InvalidOperationException("Fixture not initialized. Call Initialize first.");
}

