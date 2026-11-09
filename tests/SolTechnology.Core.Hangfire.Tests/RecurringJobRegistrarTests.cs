using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire.Tests;

[TestFixture]
public class RecurringJobRegistrarTests
{
    private const string OverlapWarning =
        "Recurring job [{JobId}] requested preventOverlap but UseSolFilters was not called; overlap is not prevented.";

    private IRecurringJobManager _manager = null!;
    private CapturingLogger<RecurringJobRegistrar> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _manager = Substitute.For<IRecurringJobManager>();
        _logger = new CapturingLogger<RecurringJobRegistrar>();
    }

    [Test]
    public async Task StartAsync_RegistersJobWithManager()
    {
        // Arrange
        var sut = CreateSut(new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *"));

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _manager.Received(1).AddOrUpdate("TestJob", Arg.Any<Job>(), "0 0 * * *", Arg.Any<RecurringJobOptions>());
    }

    [Test]
    public async Task StartAsync_MultipleDescriptors_RegistersAll()
    {
        // Arrange
        var sut = CreateSut(
            new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *"),
            new RecurringJobDescriptor(typeof(AnotherTestJob), "0 12 * * *"));

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _manager.Received(2).AddOrUpdate(Arg.Any<string>(), Arg.Any<Job>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>());
    }

    [Test]
    public async Task Registrar_PreventOverlapWithoutUseSolFilters_LogsWarning()
    {
        // Arrange
        var sut = CreateSut(new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *", preventOverlap: true));

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Template == OverlapWarning)
            .Which.Properties["JobId"].Should().Be("TestJob");
    }

    [Test]
    public async Task Registrar_PreventOverlapWithUseSolFilters_LogsNoWarning()
    {
        // Arrange
        var descriptor = new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *", preventOverlap: true);
        var overlapFilterProvider = new PreventOverlapFilterProvider([descriptor]);
        overlapFilterProvider.InstallInto(new JobFilterProviderCollection());
        var sut = new RecurringJobRegistrar(_manager, [descriptor], overlapFilterProvider, _logger);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _logger.Entries.Should().BeEmpty();
    }

    private RecurringJobRegistrar CreateSut(params RecurringJobDescriptor[] descriptors) =>
        new(_manager, descriptors, new PreventOverlapFilterProvider(descriptors), _logger);
}
