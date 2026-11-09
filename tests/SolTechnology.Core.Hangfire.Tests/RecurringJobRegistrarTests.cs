using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire.Tests;

[TestFixture]
public class RecurringJobRegistrarTests
{
    private const string OverlapWarning =
        "Recurring job [{JobId}] requested preventOverlap but UseSolFilters was not called; overlap is not prevented.";

    private const string TestJobId = "SolTechnology.Core.Hangfire.Tests.TestJob";

    private InMemoryStorage _storage = null!;
    private IRecurringJobManager _manager = null!;
    private IHostEnvironment _hostEnvironment = null!;
    private CapturingLogger<RecurringJobRegistrar> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _storage = new InMemoryStorage();
        _manager = Substitute.For<IRecurringJobManager>();
        _hostEnvironment = Substitute.For<IHostEnvironment>();
        _hostEnvironment.ApplicationName.Returns("app-a");
        _logger = new CapturingLogger<RecurringJobRegistrar>();
    }

    [TearDown]
    public void TearDown() => _storage.Dispose();

    [Test]
    public async Task StartAsync_RegistersJobWithManager()
    {
        // Arrange
        var sut = CreateSut(new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *"));

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _manager.Received(1).AddOrUpdate(
            TestJobId,
            Arg.Is<Job>(job => job.Type == typeof(RecurringJobRunner<TestJob>) && job.Method.Name == "RunAsync"),
            "0 0 * * *",
            Arg.Is<RecurringJobOptions>(options => options.TimeZone == TimeZoneInfo.Utc));
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
        var warning = _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Template == OverlapWarning).Which;
        warning.EventId.Id.Should().Be(2150);
        warning.Properties["JobId"].Should().Be(TestJobId);
    }

    [Test]
    public async Task Registrar_PreventOverlapWithUseSolFilters_LogsNoWarning()
    {
        // Arrange
        var descriptor = new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *", preventOverlap: true);
        var overlapFilterProvider = new PreventOverlapFilterProvider([descriptor]);
        overlapFilterProvider.InstallInto(new JobFilterProviderCollection());
        var sut = new RecurringJobRegistrar(_manager, [descriptor], overlapFilterProvider, Cleanup(), _logger);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _logger.Entries.Should().NotContain(e => e.EventId.Id == 2150);
    }

    [Test]
    public async Task Registrar_WithoutSmartRetryFilter_LogsWarning()
    {
        // Arrange
        var sut = CreateSut(new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *"), new RecurringJobDescriptor(typeof(AnotherTestJob), "0 0 * * *"));

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        var warning = _logger.Entries.Should().ContainSingle(e => e.EventId.Id == 2151).Which;
        warning.Level.Should().Be(LogLevel.Warning);
        warning.Template.Should().Be("[{JobCount}] recurring jobs are registered but UseSolFilters was not called; a recoverable failed Result is not retried.");
        warning.Properties["JobCount"].Should().Be(2);
    }

    [Test]
    public async Task Registrar_WithSmartRetryFilter_LogsNoSmartRetryWarning()
    {
        // Arrange
        var smartRetry = new SmartRetryJobFilter();
        GlobalJobFilters.Filters.Add(smartRetry);
        var sut = CreateSut(new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *"));

        try
        {
            // Act
            await sut.StartAsync(CancellationToken.None);
        }
        finally
        {
            GlobalJobFilters.Filters.Remove(smartRetry);
        }

        // Assert
        _logger.Entries.Should().NotContain(e => e.EventId.Id == 2151);
    }

    private RecurringJobRegistrar CreateSut(params RecurringJobDescriptor[] descriptors) =>
        new(_manager, descriptors, new PreventOverlapFilterProvider(descriptors), Cleanup(), _logger);

    private RecurringJobCleanup Cleanup() =>
        new(_manager, _storage, _hostEnvironment, Options.Create(new RecurringJobsOptions()), NullLogger<RecurringJobCleanup>.Instance);
}
