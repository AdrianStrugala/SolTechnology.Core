using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.Hangfire.Tests;

/// <summary>Removal of the <c>Type.Name</c> recurring ids that 1.x registered (P-06, RQ10).</summary>
[TestFixture]
public class RecurringJobLegacyIdTests
{
    private const string Cron = RecurringJobHost.Cron;
    private const string TestJobId = "SolTechnology.Core.Hangfire.Tests.TestJob";

    private RecurringJobHost _host = null!;

    [SetUp]
    public void Setup() => _host = new RecurringJobHost();

    [TearDown]
    public void TearDown() => _host.Dispose();

    [Test]
    public async Task Registrar_Legacy1xTypeNameId_IsRemovedOnStart()
    {
        // Arrange
        _host.SeedLegacyEntry<TestJob>("TestJob");

        // Act
        await _host.Start("app-a", s => s.AddSolRecurringJob<TestJob>(Cron));

        // Assert
        _host.StoredIds().Should().BeEquivalentTo(TestJobId);
    }

    [Test]
    public async Task Registrar_LegacyIdEqualToCurrentJobId_IsKept()
    {
        // Arrange
        _host.SeedLegacyEntry<TestJob>("TestJob");

        // Act
        await _host.Start("app-a", s => s.AddSolRecurringJob<TestJob>(Cron, jobId: "TestJob"));

        // Assert
        _host.StoredIds().Should().BeEquivalentTo("TestJob");
    }

    [Test]
    public async Task Registrar_LegacyIdOfAnotherJobType_IsKept()
    {
        // Arrange
        _host.SeedLegacyEntry<Other.TestJob>("TestJob");

        // Act
        await _host.Start("app-a", s => s.AddSolRecurringJob<TestJob>(Cron));

        // Assert
        _host.StoredIds().Should().BeEquivalentTo("TestJob", TestJobId);
    }
}
