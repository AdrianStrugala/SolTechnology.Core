using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

[TestFixture]
public class ChronicleJobFilterTests : ChronicleJobFilterTestBase
{
    [Test]
    public void OnPerformed_JobSucceeded_EmitsOneRootReportOk()
    {
        // Act
        Perform(RunnerJob<TestJob>(), Result.Success());

        // Assert
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Information);
        report.Message.Should().StartWith("Chronicle [Job TestJob] [Ok]").And.EndWith("[ok] JobId=job-1 RetryCount=0]");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OnPerformed_JobThrew_RootFailedWithInnerExceptionType(bool wrapped)
    {
        // Arrange
        var boom = new InvalidOperationException("boom");
        Exception exception = wrapped ? new JobPerformanceException("An exception occurred during performance of the job.", boom) : boom;

        // Act
        Perform(RunnerJob<TestJob>(), exception: exception);

        // Assert
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Error);
        report.Exception.Should().BeSameAs(boom);
        report.Message.Should().Contain("[failed] Error=[InvalidOperationException: boom]");
    }

    [TestCase(false, "Recoverable=False")]
    [TestCase(true, "Recoverable=True")]
    public void OnPerformed_FailedResult_RootFailedWithErrorTypeAndRecoverable(bool recoverable, string expected)
    {
        // Act
        Perform(RunnerJob<TestJob>(), Result.Fail(new NotFoundError { Message = "City [Atlantis] not found", Recoverable = recoverable }));

        // Assert
        Reports.Should().ContainSingle().Which.Message.Should().StartWith("Chronicle [Job TestJob] [Failed]")
            .And.EndWith($"[failed] Error=[NotFoundError: City [Atlantis] not found] JobId=job-1 RetryCount=0 {expected}]");
    }

    [TestCase("shutdown")]
    [TestCase("aborted")]
    public void OnPerformed_ShutdownOrAbort_RootCancelled(string reason)
    {
        // Arrange
        Exception exception = reason == "aborted" ? new JobAbortedException() : new OperationCanceledException(Shutdown.Token);
        if (reason == "shutdown")
        {
            Shutdown.Cancel();
        }

        // Act
        Perform(RunnerJob<TestJob>(), exception: exception);

        // Assert
        var report = Reports.Should().ContainSingle().Which;
        report.Level.Should().Be(LogLevel.Warning);
        report.Message.Should().StartWith("Chronicle [Job TestJob] [Cancelled]").And.NotContain("Error=");
    }

    [Test]
    public void OnPerformed_CanceledByLaterFilter_RootCancelledWithOutcome()
    {
        // Act
        Perform(RunnerJob<TestJob>(), canceled: true);

        // Assert
        Reports.Should().ContainSingle().Which.Message.Should().StartWith("Chronicle [Job TestJob] [Cancelled]")
            .And.EndWith("Outcome=CanceledByFilter]");
    }
}
