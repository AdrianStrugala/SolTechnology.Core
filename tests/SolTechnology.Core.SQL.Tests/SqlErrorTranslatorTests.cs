using FluentAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.SQL.Tests;

[TestFixture]
public class SqlErrorTranslatorTests
{
    private const int InvalidObjectName = 208;

    [Test]
    public async Task Translate_UniqueViolation_DescriptionHasNoTableName()
    {
        // Arrange
        var name = $"duplicate-{Guid.NewGuid():N}";
        await SQLTestEnvironment.InsertItem(name);

        // Act
        var result = await SqlErrorTranslator.Execute(() => SQLTestEnvironment.InsertItem(name));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error!.Description.Should().NotBeNullOrWhiteSpace();
        result.Error.Description.Should().NotContainAny("Items", "UQ_Items_Name", name);
    }

    [Test]
    public async Task Execute_UnknownSqlError_Propagates()
    {
        // Arrange
        var database = await SQLTestEnvironment.Database();

        // Act
        var execute = () => SqlErrorTranslator.Execute(async () => { await CountMissingTable(database.DatabaseConnectionString); });
        var executeWithResult = () => SqlErrorTranslator.Execute(() => CountMissingTable(database.DatabaseConnectionString));

        // Assert
        (await execute.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(InvalidObjectName);
        (await executeWithResult.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(InvalidObjectName);
    }

    private static async Task<int> CountMissingTable(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.Missing", connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}
