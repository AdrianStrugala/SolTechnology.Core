using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;

namespace SolTechnology.Core.SQL.Testing;

/// <summary>
/// Wait strategy that polls a real authenticated <c>SELECT 1</c> against <c>master</c>.
/// Connection string is resolved lazily — the mapped host port is unknown until
/// <c>StartAsync</c> assigns it.
/// </summary>
internal sealed class SqlServerLoginWaitStrategy(Func<string> connectionStringFactory) : IWaitUntil
{
    public async Task<bool> UntilAsync(IContainer container)
    {
        try
        {
            var connectionString = new SqlConnectionStringBuilder(connectionStringFactory())
            {
                InitialCatalog = "master",
                ConnectTimeout = 2
            }.ToString();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 2;
            _ = await command.ExecuteScalarAsync().ConfigureAwait(false);

            return true;
        }
        catch
        {
            return false;
        }
    }
}

