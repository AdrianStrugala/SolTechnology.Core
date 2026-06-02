namespace SolTechnology.Core.SQL.Testing.Provisioning;

/// <summary>
/// Executes raw <c>.sql</c> script files against the application database, in order. Works for both
/// engines (the scripts must be dialect-appropriate).
/// </summary>
internal sealed class ScriptProvisioner(IReadOnlyList<string> scriptPaths) : ISchemaProvisioner
{
    public bool CreatesDatabase => false;

    public async Task ProvisionAsync(SQLFixture fixture, CancellationToken ct)
    {
        await using var connection = fixture.CreateDatabaseConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);

        foreach (var path in scriptPaths)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("SQL script not found", path);
            }

            var sql = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }
}


