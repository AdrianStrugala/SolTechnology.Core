using System.Diagnostics;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.SQL.Connections;

namespace SolTechnology.Core.SQL.Tests;

[TestFixture]
public class SQLConnectionFactoryTests
{
    private const int LoginFailed = 18456;

    [Test]
    public async Task CreateConnection_LoginFailed_FailsWithoutRetry()
    {
        // Arrange
        var database = await SQLTestEnvironment.Database();
        var wrongPassword = new SqlConnectionStringBuilder(database.DatabaseConnectionString)
        {
            Password = "wrong-password",
            Pooling = false
        }.ToString();
        await using var provider = new ServiceCollection()
            .AddSolSQL(new SQLConfiguration { ConnectionString = wrongPassword })
            .BuildServiceProvider();
        var sut = provider.GetRequiredService<ISQLConnectionFactory>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var exception = Capture(() => sut.CreateConnection());
        stopwatch.Stop();

        // Assert
        exception.Should().BeOfType<SqlException>().Which.Number.Should().Be(LoginFailed);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
