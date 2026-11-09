using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.Server;
using Hangfire.Storage;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

[TestFixture]
public class PreventOverlapJobFilterTests
{
    private InMemoryStorage _storage = null!;
    private PreventOverlapJobFilter _sut = null!;

    [SetUp]
    public void Setup()
    {
        _storage = new InMemoryStorage();
        _sut = new PreventOverlapJobFilter();
    }

    [TearDown]
    public void TearDown() => _storage.Dispose();

    [Test]
    public void OnPerforming_LockFree_DoesNotCancel()
    {
        // Arrange
        var context = CreatePerformingContext(Job.FromExpression(() => Console.WriteLine("test")), "nightly");

        // Act
        _sut.OnPerforming(context);

        // Assert
        context.Canceled.Should().BeFalse();
    }

    [Test]
    public void OnPerforming_LockHeld_CancelsRun()
    {
        // Arrange
        using var otherServer = _storage.GetConnection();
        using var heldLock = otherServer.AcquireDistributedLock("sol:overlap:nightly", TimeSpan.Zero);
        var context = CreatePerformingContext(Job.FromExpression(() => Console.WriteLine("test")), "nightly");

        // Act
        _sut.OnPerforming(context);

        // Assert
        context.Canceled.Should().BeTrue();
    }

    [Test]
    public void OnPerformed_ReleasesLock()
    {
        // Arrange
        var context = CreatePerformingContext(Job.FromExpression(() => Console.WriteLine("test")), "nightly");
        _sut.OnPerforming(context);

        // Act
        _sut.OnPerformed(new PerformedContext(context, null, false, null));

        // Assert
        using var otherServer = _storage.GetConnection();
        var acquire = () => otherServer.AcquireDistributedLock("sol:overlap:nightly", TimeSpan.Zero).Dispose();
        acquire.Should().NotThrow();
    }

    [Test]
    public void OnPerforming_SameInvocationProcessing_CancelsRun()
    {
        // Arrange
        var running = CreatePerformingContext(Job.FromExpression(() => Console.WriteLine("same")));
        var next = CreatePerformingContext(Job.FromExpression(() => Console.WriteLine("same")));
        _sut.OnPerforming(running);

        // Act
        _sut.OnPerforming(next);

        // Assert
        running.Canceled.Should().BeFalse();
        next.Canceled.Should().BeTrue();
    }

    [Test]
    public void OnPerforming_DifferentArgumentsProcessing_DoesNotCancel()
    {
        // Arrange
        var running = CreatePerformingContext(Job.FromExpression(() => Console.WriteLine("first")));
        var next = CreatePerformingContext(Job.FromExpression(() => Console.WriteLine("second")));
        _sut.OnPerforming(running);

        // Act
        _sut.OnPerforming(next);

        // Assert
        next.Canceled.Should().BeFalse();
    }

    private PerformingContext CreatePerformingContext(Job job, string? recurringJobId = null)
    {
        var connection = _storage.GetConnection();
        var parameters = new Dictionary<string, string?>();
        if (recurringJobId is not null)
        {
            parameters["RecurringJobId"] = SerializationHelper.Serialize(recurringJobId);
        }

        var jobId = connection.CreateExpiredJob(job, parameters, DateTime.UtcNow, TimeSpan.FromHours(1));
        var backgroundJob = new BackgroundJob(jobId, job, DateTime.UtcNow);
        return new PerformingContext(new PerformContext(_storage, connection, backgroundJob, Substitute.For<IJobCancellationToken>()));
    }
}
