using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

[TestFixture]
public class SmartRetryJobFilterTests
{
    private SmartRetryJobFilter _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new SmartRetryJobFilter();
    }

    [Test]
    public void OnPerformed_WhenResultIsRecoverableFailure_SetsRetryParameter()
    {
        // Arrange
        var context = CreatePerformedContext(
            result: Result.Fail(new Error { Message = "Timeout", Recoverable = true }));

        // Act
        _sut.OnPerformed(context);

        // Assert
        var errorParam = context.GetJobParameter<string>("SmartRetry_Error");
        errorParam.Should().Be("Timeout");
    }

    [Test]
    public void OnPerformed_WhenResultIsNonRecoverableFailure_DoesNotSetParameter()
    {
        // Arrange
        var context = CreatePerformedContext(
            result: Result.Fail(new Error { Message = "Bad data", Recoverable = false }));

        // Act
        _sut.OnPerformed(context);

        // Assert
        var errorParam = context.GetJobParameter<string>("SmartRetry_Error");
        errorParam.Should().BeNull();
    }

    [Test]
    public void OnPerformed_WhenResultIsSuccess_DoesNotSetParameter()
    {
        // Arrange
        var context = CreatePerformedContext(result: Result.Success());

        // Act
        _sut.OnPerformed(context);

        // Assert
        var errorParam = context.GetJobParameter<string>("SmartRetry_Error");
        errorParam.Should().BeNull();
    }

    [Test]
    public void OnPerformed_WhenExceptionWasThrown_DoesNothing()
    {
        // Arrange
        var context = CreatePerformedContext(
            result: null,
            exception: new InvalidOperationException("boom"));

        // Act & Assert
        var act = () => _sut.OnPerformed(context);
        act.Should().NotThrow();
    }

    [Test]
    public void OnStateElection_WhenRetryErrorSet_ForcesFailedState()
    {
        // Arrange
        var context = CreateElectStateContext();
        context.SetJobParameter("SmartRetry_Error", "Timeout error");

        // Act
        _sut.OnStateElection(context);

        // Assert
        context.CandidateState.Should().BeOfType<FailedState>();
        ((FailedState)context.CandidateState).Exception.Message.Should().Be("Timeout error");
    }

    [Test]
    public void OnStateElection_WhenNoRetryError_DoesNotChangeCandidateState()
    {
        // Arrange
        var context = CreateElectStateContext();
        var originalState = context.CandidateState;

        // Act
        _sut.OnStateElection(context);

        // Assert
        context.CandidateState.Should().BeSameAs(originalState);
    }

    private static PerformedContext CreatePerformedContext(object? result, Exception? exception = null)
    {
        var storage = Substitute.For<JobStorage>();
        var connection = Substitute.For<IStorageConnection>();
        var job = Job.FromExpression(() => Console.WriteLine("test"));
        var backgroundJob = new BackgroundJob("job-1", job, DateTime.UtcNow);
        var performContext = new PerformContext(storage, connection, backgroundJob, Substitute.For<IJobCancellationToken>());
        var performingContext = new PerformingContext(performContext);
        return new PerformedContext(performingContext, result, false, exception);
    }

    private static ElectStateContext CreateElectStateContext()
    {
        var storage = Substitute.For<JobStorage>();
        var connection = Substitute.For<IStorageConnection>();
        var job = Job.FromExpression(() => Console.WriteLine("test"));
        var backgroundJob = new BackgroundJob("job-1", job, DateTime.UtcNow);
        var candidateState = new EnqueuedState();
        return new ElectStateContext(new ApplyStateContext(storage, connection, null, backgroundJob, candidateState, null));
    }
}


