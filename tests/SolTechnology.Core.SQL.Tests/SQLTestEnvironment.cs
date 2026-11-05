using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.SQL.Testing;

namespace SolTechnology.Core.SQL.Tests;

[SetUpFixture]
public static class SQLTestEnvironment
{
    private static readonly Lazy<Task<SQLFixture>> Fixture = new(StartFixture);

    public static Task<SQLFixture> Database() => Fixture.Value;

    public static async Task<ServiceProvider> BuildProvider()
    {
        var database = await Database();
        var services = new ServiceCollection().AddLogging();
        services.AddSolSQL(new SQLConfiguration { ConnectionString = database.DatabaseConnectionString });
        return services.BuildServiceProvider();
    }

    public static async Task InsertItem(string name)
    {
        var database = await Database();
        await using var connection = new SqlConnection(database.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("INSERT INTO dbo.Items (Name) VALUES (@name)", connection);
        command.Parameters.AddWithValue("@name", name);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<int> CountItems(string name)
    {
        var database = await Database();
        await using var connection = new SqlConnection(database.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.Items WHERE Name = @name", connection);
        command.Parameters.AddWithValue("@name", name);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    [OneTimeTearDown]
    public static async Task TearDown()
    {
        if (Fixture.IsValueCreated)
        {
            await (await Fixture.Value).DisposeAsync();
        }
    }

    private static async Task<SQLFixture> StartFixture()
    {
        var fixture = new SQLFixture("SolSQLTests").WithSchema(CreateSchema);
        await fixture.InitializeAsync();
        return fixture;
    }

    private static async Task CreateSchema(string connectionString, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(
            "CREATE TABLE dbo.Items (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(100) NOT NULL CONSTRAINT UQ_Items_Name UNIQUE)",
            connection);
        await command.ExecuteNonQueryAsync(ct);
    }
}
