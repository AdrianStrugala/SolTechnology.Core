using Respawn;
using SolTechnology.Core.SQL.Testing.Engines;

namespace SolTechnology.Core.SQL.Testing;

/// <summary>
/// Respawn-based reset of the application database to empty (schema preserved). Scoped to the
/// application catalog only, so a shared MSSQL instance hosting a Service Bus emulator catalog is
/// never truncated. The <see cref="Respawner"/> is created lazily on first reset and reused.
/// </summary>
internal sealed class SQLReset(IDatabaseEngine engine, string databaseConnectionString)
{
    private Respawner? _respawner;

    public async Task ResetAsync(CancellationToken ct = default)
    {
        await using var connection = engine.OpenConnection(databaseConnectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        _respawner ??= await Respawner.CreateAsync(connection, engine.BuildResetOptions()).ConfigureAwait(false);
        await _respawner.ResetAsync(connection).ConfigureAwait(false);
    }
}


