using System.Data.Common;
using DotNet.Testcontainers.Networks;
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
public sealed class SQLFixture : IAsyncDisposable
{
    private readonly string? _image;
    private readonly string _containerName;

    private IDatabaseEngine? _engine;
    private ISchemaProvisioner? _provisioner;
    private SQLReset? _reset;
    private INetwork? _network;
    private string? _networkAlias;

    public SQLFixture(string databaseName, string? image = null)
    {
        DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
        _image = image;
        _containerName = $"sol-sql-{databaseName.ToLowerInvariant()}";
    }

    /// <summary>The application database name.</summary>
    public string DatabaseName { get; }

    /// <summary>The engine backing this fixture. Defaults to <see cref="SQLProvider.SQLServer"/>.</summary>
    public SQLProvider Provider { get; private set; } = SQLProvider.SQLServer;

    /// <summary>Server-level connection string (no specific catalog). Valid after <see cref="InitializeAsync"/>.</summary>
    public string ConnectionString => Engine.ServerConnectionString;

    /// <summary>Connection string targeting the application database. Valid after <see cref="InitializeAsync"/>.</summary>
    public string DatabaseConnectionString => Engine.DatabaseConnectionString(DatabaseName);


    // ---- fluent configuration ----

    /// <summary>Use PostgreSQL instead of the default SQL Server. Call before <see cref="InitializeAsync"/>.</summary>
    public SQLFixture UsePostgres()
    {
        Provider = SQLProvider.Postgres;
        return this;
    }

    /// <summary>Provision the schema by building and deploying a dacpac from a <c>.sqlproj</c> (SQL Server only).</summary>
    public SQLFixture WithSQLProject(string sqlProjPath)
    {
        _provisioner = new DacpacProvisioner(sqlProjPath);
        return this;
    }

    /// <summary>Provision the schema by executing raw <c>.sql</c> script files, in order.</summary>
    public SQLFixture WithScripts(params string[] scriptPaths)
    {
        _provisioner = new ScriptProvisioner(scriptPaths);
        return this;
    }

    /// <summary>
    /// Provision the schema with a delegate that receives the application database connection string.
    /// This is the EF-migrations seam: the package stays EF-free; the consumer runs migrations with
    /// their own <c>DbContext</c>.
    /// </summary>
    public SQLFixture WithEfMigrations(Func<string, Task> applyMigrations)
    {
        ArgumentNullException.ThrowIfNull(applyMigrations);
        _provisioner = new DelegateProvisioner((cs, _) => applyMigrations(cs));
        return this;
    }

    /// <summary>General provisioning seam: a delegate receiving the application connection string and a token.</summary>
    public SQLFixture WithSchema(Func<string, CancellationToken, Task> provision)
    {
        _provisioner = new DelegateProvisioner(provision);
        return this;
    }

    /// <summary>Attach the container to a docker network (used to share MSSQL with the Service Bus emulator).</summary>
    public SQLFixture WithNetwork(INetwork network, string? alias = null)
    {
        _network = network;
        _networkAlias = alias;
        return this;
    }

    // ---- lifecycle ----

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_provisioner is null)
        {
            throw new InvalidOperationException(
                "Configure schema provisioning (WithSQLProject / WithScripts / WithEfMigrations / WithSchema) before InitializeAsync.");
        }

        _engine = Provider switch
        {
            SQLProvider.Postgres => new PostgresEngine(_image, DatabaseName, _containerName),
            _ => new MSSQLEngine(_image, _containerName)
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
    public Task ResetAsync(CancellationToken ct = default)
    {
        if (_reset is null)
        {
            throw new InvalidOperationException("Call InitializeAsync before ResetAsync.");
        }

        return _reset.ResetAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_engine is not null)
        {
            await _engine.DisposeAsync().ConfigureAwait(false);
        }
    }

    // ---- internal helpers used by provisioners ----
    internal DbConnection CreateDatabaseConnection() => Engine.OpenConnection(DatabaseConnectionString);

    private IDatabaseEngine Engine =>
        _engine ?? throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
}

