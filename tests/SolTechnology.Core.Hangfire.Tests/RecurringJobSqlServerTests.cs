using FluentAssertions;
using Hangfire.SqlServer;
using NUnit.Framework;
using SolTechnology.Core.SQL.Testing;

namespace SolTechnology.Core.Hangfire.Tests;

/// <summary>
/// Hangfire.SqlServer keys are nvarchar(100): a long default job id and a long application name must still register.
/// </summary>
[TestFixture]
public class RecurringJobSqlServerTests
{
    private SQLFixture _database = null!;
    private RecurringJobHost _host = null!;

    [OneTimeSetUp]
    public async Task StartSqlServer()
    {
        _database = new SQLFixture("SolHangfireTests").WithSchema((_, _) => Task.CompletedTask);
        await _database.InitializeAsync();
        _host = new RecurringJobHost(new SqlServerStorage(_database.DatabaseConnectionString));
    }

    [OneTimeTearDown]
    public async Task StopSqlServer()
    {
        _host.Dispose();
        await _database.DisposeAsync();
    }

    [Test]
    public async Task AddSolRecurringJob_LongGenericTypeName_RegistersOnSqlServer()
    {
        // Arrange
        var applicationName = new string('a', 120);

        // Act
        await _host.Start(applicationName, s => s.AddSolRecurringJob<GenericJob<List<string>>>(RecurringJobHost.Cron), removeStaleJobs: true);

        // Assert
        _host.StoredIds().Should().Contain("GenericJob`1-ac9290f02c71");
    }
}
