using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

[TestFixture]
public class PreventOverlapJobFilterTests
{
    private PreventOverlapJobFilter _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new PreventOverlapJobFilter();
    }

    [Test]
    public void OnPerforming_WhenNoMatchingJobs_DoesNotCancel()
    {
        // Arrange
        var monitoringApi = Substitute.For<IMonitoringApi>();
        monitoringApi.ScheduledJobs(0, int.MaxValue)
            .Returns(new JobList<ScheduledJobDto>(new List<KeyValuePair<string, ScheduledJobDto>>()));
        monitoringApi.ProcessingJobs(0, int.MaxValue)
            .Returns(new JobList<ProcessingJobDto>(new List<KeyValuePair<string, ProcessingJobDto>>()));

        var context = CreatePerformingContext(monitoringApi);

        // Act
        _sut.OnPerforming(context);

        // Assert
        context.Canceled.Should().BeFalse();
    }

    [Test]
    public void OnPerforming_WhenMatchingScheduledJobExists_CancelsExecution()
    {
        // Arrange
        var currentJob = Job.FromExpression(() => Console.WriteLine("test"));
        var matchingJob = Job.FromExpression(() => Console.WriteLine("test"));

        var monitoringApi = Substitute.For<IMonitoringApi>();
        monitoringApi.ScheduledJobs(0, int.MaxValue)
            .Returns(new JobList<ScheduledJobDto>(new List<KeyValuePair<string, ScheduledJobDto>>
            {
                new("other-job-id", new ScheduledJobDto { Job = matchingJob, EnqueueAt = DateTime.UtcNow })
            }));
        monitoringApi.ProcessingJobs(0, int.MaxValue)
            .Returns(new JobList<ProcessingJobDto>(new List<KeyValuePair<string, ProcessingJobDto>>()));

        var context = CreatePerformingContext(monitoringApi, currentJob, "current-job-id");

        // Act
        _sut.OnPerforming(context);

        // Assert
        context.Canceled.Should().BeTrue();
    }

    [Test]
    public void OnPerforming_WhenMatchingProcessingJobExists_CancelsExecution()
    {
        // Arrange
        var currentJob = Job.FromExpression(() => Console.WriteLine("test"));
        var matchingJob = Job.FromExpression(() => Console.WriteLine("test"));

        var monitoringApi = Substitute.For<IMonitoringApi>();
        monitoringApi.ScheduledJobs(0, int.MaxValue)
            .Returns(new JobList<ScheduledJobDto>(new List<KeyValuePair<string, ScheduledJobDto>>()));
        monitoringApi.ProcessingJobs(0, int.MaxValue)
            .Returns(new JobList<ProcessingJobDto>(new List<KeyValuePair<string, ProcessingJobDto>>
            {
                new("other-job-id", new ProcessingJobDto { Job = matchingJob, ServerId = "srv-1", StartedAt = DateTime.UtcNow })
            }));

        var context = CreatePerformingContext(monitoringApi, currentJob, "current-job-id");

        // Act
        _sut.OnPerforming(context);

        // Assert
        context.Canceled.Should().BeTrue();
    }

    [Test]
    public void OnPerforming_WhenSameJobIdInProcessing_DoesNotCancel()
    {
        // Arrange
        var currentJob = Job.FromExpression(() => Console.WriteLine("test"));

        var monitoringApi = Substitute.For<IMonitoringApi>();
        monitoringApi.ScheduledJobs(0, int.MaxValue)
            .Returns(new JobList<ScheduledJobDto>(new List<KeyValuePair<string, ScheduledJobDto>>()));
        monitoringApi.ProcessingJobs(0, int.MaxValue)
            .Returns(new JobList<ProcessingJobDto>(new List<KeyValuePair<string, ProcessingJobDto>>
            {
                new("current-job-id", new ProcessingJobDto { Job = currentJob, ServerId = "srv-1", StartedAt = DateTime.UtcNow })
            }));

        var context = CreatePerformingContext(monitoringApi, currentJob, "current-job-id");

        // Act
        _sut.OnPerforming(context);

        // Assert
        context.Canceled.Should().BeFalse();
    }

    [Test]
    public void OnPerforming_WhenDifferentMethodInScheduled_DoesNotCancel()
    {
        // Arrange
        var currentJob = Job.FromExpression(() => Console.WriteLine("test"));
        var differentJob = Job.FromExpression(() => Console.Write("different"));

        var monitoringApi = Substitute.For<IMonitoringApi>();
        monitoringApi.ScheduledJobs(0, int.MaxValue)
            .Returns(new JobList<ScheduledJobDto>(new List<KeyValuePair<string, ScheduledJobDto>>
            {
                new("other-id", new ScheduledJobDto { Job = differentJob, EnqueueAt = DateTime.UtcNow })
            }));
        monitoringApi.ProcessingJobs(0, int.MaxValue)
            .Returns(new JobList<ProcessingJobDto>(new List<KeyValuePair<string, ProcessingJobDto>>()));

        var context = CreatePerformingContext(monitoringApi, currentJob, "current-job-id");

        // Act
        _sut.OnPerforming(context);

        // Assert
        context.Canceled.Should().BeFalse();
    }

    private static PerformingContext CreatePerformingContext(
        IMonitoringApi monitoringApi,
        Job? job = null,
        string jobId = "job-1")
    {
        job ??= Job.FromExpression(() => Console.WriteLine("test"));
        var storage = Substitute.For<JobStorage>();
        storage.GetMonitoringApi().Returns(monitoringApi);
        var connection = Substitute.For<IStorageConnection>();
        var backgroundJob = new BackgroundJob(jobId, job, DateTime.UtcNow);
        return new PerformingContext(new PerformContext(storage, connection, backgroundJob, Substitute.For<IJobCancellationToken>()));
    }
}

