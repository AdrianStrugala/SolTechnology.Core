using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace SolTechnology.Core.Hangfire.Tests;

/// <summary>Recurring job ids across host starts on a shared storage (P-06, premortem RM-3, RM-8).</summary>
[TestFixture]
public class RecurringJobIdTests
{
    private const string Cron = RecurringJobHost.Cron;
    private const string TestJobId = "SolTechnology.Core.Hangfire.Tests.TestJob";
    private const string AnotherTestJobId = "SolTechnology.Core.Hangfire.Tests.AnotherTestJob";

    private RecurringJobHost _host = null!;

    [SetUp]
    public void Setup() => _host = new RecurringJobHost();

    [TearDown]
    public void TearDown() => _host.Dispose();

    [Test]
    public async Task AddSolRecurringJob_SameNameDifferentNamespace_TwoJobs()
    {
        // Act
        await _host.Start("app-a", s => s.AddSolRecurringJob<TestJob>(Cron).AddSolRecurringJob<Other.TestJob>(Cron));

        // Assert
        _host.StoredIds().Should().BeEquivalentTo(TestJobId, "SolTechnology.Core.Hangfire.Tests.Other.TestJob");
    }

    [Test]
    public void AddSolRecurringJob_GenericJobType_IdHasNoAssemblyVersion()
    {
        // Act
        var descriptor = new ServiceCollection().AddSolRecurringJob<GenericJob<List<string>>>(Cron)
            .BuildServiceProvider().GetRequiredService<RecurringJobDescriptor>();

        // Assert: the 96-character full name is shortened to the type name plus a SHA-256 prefix of it.
        descriptor.JobId.Should().Be("GenericJob`1-ac9290f02c71");
        descriptor.JobId.Length.Should().BeLessThanOrEqualTo(StorageKey.MaxRecurringJobIdLength);
    }

    [Test]
    public void AddSolRecurringJob_ShortGenericJobType_IdIsFullTypeName()
    {
        // Act
        var descriptor = new ServiceCollection().AddSolRecurringJob<GenericJob<string>>(Cron)
            .BuildServiceProvider().GetRequiredService<RecurringJobDescriptor>();

        // Assert
        descriptor.JobId.Should().Be("SolTechnology.Core.Hangfire.Tests.GenericJob`1[System.String]");
    }

    [Test]
    public async Task AddSolRecurringJob_CustomIdAndTimeZone_StoredOnEntry()
    {
        // Arrange
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

        // Act
        await _host.Start("app-a", s => s.AddSolRecurringJob<TestJob>(Cron, jobId: "nightly", timeZone: warsaw));

        // Assert
        _host.StoredJobs().Should().ContainSingle(j => j.Id == "nightly").Which.TimeZoneId.Should().Be(warsaw.Id);
    }

    [Test]
    public async Task Registrar_RemovedJob_IsDeletedFromStorage()
    {
        // Arrange
        await _host.Start("app-a", s => s.AddSolRecurringJob<TestJob>(Cron).AddSolRecurringJob<AnotherTestJob>(Cron));

        // Act
        await _host.Start("app-a", s => s.AddSolRecurringJob<TestJob>(Cron));

        // Assert
        _host.StoredIds().Should().BeEquivalentTo(TestJobId);
    }

    [Test]
    public async Task Registrar_TwoApplicationsSharedStorage_KeepEachOthersJobs()
    {
        // Arrange
        await _host.Start("app-a", s => s.AddSolRecurringJob<TestJob>(Cron));
        await _host.Start("app-b", s => s.AddSolRecurringJob<AnotherTestJob>(Cron));

        // Act
        await _host.Start("app-a", s => s.AddSolRecurringJob<TestJob>(Cron));
        await _host.Start("app-b", s => s.AddSolRecurringJob<AnotherTestJob>(Cron));

        // Assert
        _host.StoredIds().Should().BeEquivalentTo(TestJobId, AnotherTestJobId);
    }
}
