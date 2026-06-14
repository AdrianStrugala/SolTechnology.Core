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
        var failedResult = Result.Fail(new Error { Message = "Timeout", Recoverable = true });
        var connection = Substitute.For<IStorageConnection>();
        var context = CreatePerformedContext(failedResult, connection: connection);

        // Act
        _sut.OnPerformed(context);

        // Assert — the filter wrote the retry parameter to the connection
        connection.Received(1).SetJobParameter("job-1", "SmartRetry_Error", Arg.Is<string>(s => s.Contains("Timeout")));
    }

    [Test]
    public void OnPerformed_WhenResultIsNonRecoverableFailure_DoesNotSetParameter()
    {
        // Arrange
        var connection = Substitute.For<IStorageConnection>();
        var context = CreatePerformedContext(
            result: Result.Fail(new Error { Message = "Bad data", Recoverable = false }), connection: connection);

        // Act
        _sut.OnPerformed(context);

        // Assert
        connection.DidNotReceive().SetJobParameter(Arg.Any<string>(), "SmartRetry_Error", Arg.Any<string>());
    }

    [Test]
    public void OnPerformed_WhenResultIsSuccess_DoesNotSetParameter()
    {
        // Arrange
        var connection = Substitute.For<IStorageConnection>();
        var context = CreatePerformedContext(result: Result.Success(), connection: connection);

        // Act
        _sut.OnPerformed(context);

        // Assert
        connection.DidNotReceive().SetJobParameter(Arg.Any<string>(), "SmartRetry_Error", Arg.Any<string>());
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
        var connection = Substitute.For<IStorageConnection>();
        connection.GetJobParameter("job-1", "SmartRetry_Error")
            .Returns("\"Timeout error\"");
        var context = CreateElectStateContext(connection);

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

    private static PerformedContext CreatePerformedContext(object? result, Exception? exception = null, IStorageConnection? connection = null)
    {
        var storage = Substitute.For<JobStorage>();
        connection ??= Substitute.For<IStorageConnection>();
        var job = Job.FromExpression(() => Console.WriteLine("test"));
        var backgroundJob = new BackgroundJob("job-1", job, DateTime.UtcNow);
        var performContext = new PerformContext(storage, connection, backgroundJob, Substitute.For<IJobCancellationToken>());
        var performingContext = new PerformingContext(performContext);
        return new PerformedContext(performingContext, result, false, exception);
    }
    private static ElectStateContext CreateElectStateContext(IStorageConnection? connection = null)
    {
        var storage = Substitute.For<JobStorage>();
        connection ??= Substitute.For<IStorageConnection>();
        var transaction = Substitute.For<IWriteOnlyTransaction>();
        var job = Job.FromExpression(() => Console.WriteLine("test"));
        var backgroundJob = new BackgroundJob("job-1", job, DateTime.UtcNow);
        var candidateState = new EnqueuedState();
        return new ElectStateContext(new ApplyStateContext(storage, connection, transaction, backgroundJob, candidateState, null));
    }
}


