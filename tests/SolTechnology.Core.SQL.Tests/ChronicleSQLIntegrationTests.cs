using System.Data;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.SQL.Connections;

namespace SolTechnology.Core.SQL.Tests;

[TestFixture]
public class ChronicleSQLIntegrationTests
{
    private readonly FakeLogCollector _logs = new();
    private ServiceProvider _provider = null!;

    [SetUp]
    public async Task StartChronicle()
    {
        var database = await SQLTestEnvironment.Database();
        _logs.Clear();
        _provider = new ServiceCollection()
            .AddLogging(logging => logging.AddProvider(new FakeLoggerProvider(_logs)))
            .AddSolSQL(new SQLConfiguration { ConnectionString = database.DatabaseConnectionString })
            .AddSolChronicle()
            .BuildServiceProvider();
        foreach (var service in _provider.GetServices<IHostedService>())
        {
            await service.StartAsync(CancellationToken.None);
        }
    }

    [TearDown]
    public async Task StopChronicle()
    {
        foreach (var service in _provider.GetServices<IHostedService>())
        {
            await service.StopAsync(CancellationToken.None);
        }

        await _provider.DisposeAsync();
    }

    [Test]
    public void Query_InsideRoot_AppearsAsSQLStep()
    {
        // Arrange
        var chronicle = _provider.GetRequiredService<IChronicle>();
        var connections = _provider.GetRequiredService<ISQLConnectionFactory>();

        // Act
        using (chronicle.Begin("CountItems"))
        {
            using var connection = connections.CreateConnection();
            Execute(connection, "SELECT COUNT(*) FROM dbo.Items", command => command.ExecuteScalar());
        }

        // Assert
        var report = _logs.GetSnapshot().Should().ContainSingle(r => r.Id.Id == 2154).Subject.Message;
        report.Should().MatchRegex(@"\n`- SQL SELECT \[@\d+ ms\] \[\d+ ms\] \[ok\]");
        report.Should().NotContain("dbo.Items");
    }

    [Test]
    public void Reader_RowStreaming_NotIncludedInStepDuration()
    {
        // Arrange
        var chronicle = _provider.GetRequiredService<IChronicle>();
        var connections = _provider.GetRequiredService<ISQLConnectionFactory>();

        // Act
        using (chronicle.Begin("ReadObjects"))
        {
            using var connection = connections.CreateConnection();
            Execute(connection, "SELECT TOP (3) name FROM sys.objects", command =>
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Thread.Sleep(150);
                }
            });
        }

        // Assert
        var report = _logs.GetSnapshot().Should().ContainSingle(r => r.Id.Id == 2154).Subject.Message;
        var sqlDurationMs = int.Parse(Regex.Match(report, @"SQL SELECT \[@\d+ ms\] \[(\d+) ms\]").Groups[1].Value);
        TestContext.Out.WriteLine($"SQL step [{sqlDurationMs}] ms; rows were read over at least 450 ms");
        sqlDurationMs.Should().BeLessThan(450);
    }

    private static void Execute(IDbConnection connection, string text, Action<IDbCommand> run)
    {
        using var command = connection.CreateCommand();
        command.CommandText = text;
        run(command);
    }
}
