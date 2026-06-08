using System.Data.Common;
using DotNet.Testcontainers.Networks;
using Respawn;

namespace SolTechnology.Core.SQL.Testing.Engines;

/// <summary>
/// Database-engine seam consumed by <see cref="SQLFixture"/>. Each implementation owns its container
/// lifecycle and the engine-specific bits (image, connection strings, ADO provider, Respawn adapter)
/// so the fixture, provisioners and reset stay engine-agnostic.
/// </summary>
internal interface IDatabaseEngine
{

    /// <summary>Server-level connection string with no specific catalog (admin login).</summary>
    string ServerConnectionString { get; }

    /// <summary>Connection string targeting <paramref name="databaseName"/>.</summary>
    string DatabaseConnectionString(string databaseName);

    /// <summary>Starts the container, applying reuse policy when enabled.</summary>
    Task StartAsync(INetwork? network, string? networkAlias, CancellationToken ct);

    /// <summary>Opens an ADO connection for the engine (SqlConnection / NpgsqlConnection).</summary>
    DbConnection OpenConnection(string connectionString);

    /// <summary>
    /// Ensures the application catalog exists, creating it if necessary. No-op for engines that create
    /// the database when the container starts (Postgres) or via the provisioner (MSSQL dacpac); needed
    /// for MSSQL script / delegate provisioning, where only <c>master</c> exists on a fresh container.
    /// </summary>
    Task EnsureDatabaseAsync(string databaseName, CancellationToken ct);

    /// <summary>Respawn options (adapter + schema scoping) for resetting the application catalog.</summary>
    RespawnerOptions BuildResetOptions();

    ValueTask DisposeAsync();
}

