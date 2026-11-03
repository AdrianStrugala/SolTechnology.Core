using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SolTechnology.Core.CQRS.PipelineBehaviors;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Masking;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.CQRS.Tests.PipelineBehaviors;

[TestFixture]
public class LoggingPipelineBehaviorTests
{
    private const int OperationSucceededEventId = 2138;
    private const int OperationFailedWithResultEventId = 2149;

    private sealed record ListenerProbeQuery(string Name);

    private sealed record FailingProbeCommand(string Name);

    private sealed record SucceedingProbeCommand(string Name);

    private sealed record ValidatedProbeQuery(string Email);

    private sealed record ScopedProbeCommand(
        [property: LogScope] string CityName,
        [property: LogScope("UserEmail"), Masked] string Email);

    private RecordingLoggerProvider _loggerProvider = null!;
    private ILoggerFactory _loggerFactory = null!;
    private ConcurrentBag<Activity> _stoppedActivities = null!;
    private ActivityListener _listener = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerProvider = new RecordingLoggerProvider();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_loggerProvider));
        _stoppedActivities = [];
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CoreLoggingActivitySources.OperationsName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _stoppedActivities.Add
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [TearDown]
    public void TearDown()
    {
        _listener.Dispose();
        _loggerFactory.Dispose();
        _loggerProvider.Dispose();
    }

    [Test]
    public async Task LoggingPipelineBehavior_ListenerOnOperationsName_ReceivesActivity()
    {
        // Arrange
        var sut = new LoggingPipelineBehavior<ListenerProbeQuery, string>(
            NullLogger<LoggingPipelineBehavior<ListenerProbeQuery, string>>.Instance);

        // Act
        var response = await sut.Handle(new ListenerProbeQuery("Warsaw"), () => Task.FromResult("ok"), CancellationToken.None);

        // Assert
        response.Should().Be("ok");
        var activity = StoppedActivity(nameof(ListenerProbeQuery));
        activity.Source.Name.Should().Be(CoreLoggingActivitySources.OperationsName);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Test]
    public async Task Handle_ResultFail_LogsFailureAndErrorStatus()
    {
        // Arrange
        var sut = CreateSut<FailingProbeCommand, Result>();
        var error = new NotFoundError { Message = "City [Atlantis] not found" };

        // Act
        var response = await sut.Handle(new FailingProbeCommand("Atlantis"), () => Task.FromResult(Result.Fail(error)), CancellationToken.None);

        // Assert
        response.IsFailure.Should().BeTrue();
        _loggerProvider.Logs.Should().NotContain(l => l.EventId.Id == OperationSucceededEventId);
        var failure = _loggerProvider.Logs.Should().ContainSingle(l => l.EventId.Id == OperationFailedWithResultEventId).Subject;
        failure.Level.Should().Be(LogLevel.Warning);
        failure.Exception.Should().BeNull();
        Property(failure, "OperationName").Should().Be(nameof(FailingProbeCommand));
        Property(failure, "ErrorType").Should().Be(nameof(NotFoundError));
        Property(failure, "ErrorMessage").Should().Be("City [Atlantis] not found");
        StoppedActivity(nameof(FailingProbeCommand)).Status.Should().Be(ActivityStatusCode.Error);
    }

    [Test]
    public async Task Handle_ResultSuccess_LogsSucceeded()
    {
        // Arrange
        var sut = CreateSut<SucceedingProbeCommand, Result>();

        // Act
        await sut.Handle(new SucceedingProbeCommand("Warsaw"), () => Result.SuccessAsTask(), CancellationToken.None);

        // Assert
        var succeeded = _loggerProvider.Logs.Should().ContainSingle(l => l.EventId.Id == OperationSucceededEventId).Subject;
        succeeded.Level.Should().Be(LogLevel.Information);
        Property(succeeded, "OperationName").Should().Be(nameof(SucceedingProbeCommand));
        _loggerProvider.Logs.Should().NotContain(l => l.Level >= LogLevel.Warning);
        StoppedActivity(nameof(SucceedingProbeCommand)).Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Test]
    public async Task Handle_LogScopeWithMaskedProperty_MasksValue()
    {
        // Arrange
        var sut = CreateSut<ScopedProbeCommand, Result>();

        // Act
        await sut.Handle(new ScopedProbeCommand("Warsaw", "jan.kowalski@example.com"), () => Result.SuccessAsTask(), CancellationToken.None);

        // Assert
        var scope = _loggerProvider.Scopes.Should().ContainSingle().Subject
            .Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        scope.Should().Contain("CityName", "Warsaw");
        scope.Should().Contain("UserEmail", LoggingDefaults.MaskedValue);
        StoppedActivity(nameof(ScopedProbeCommand)).GetTagItem("UserEmail").Should().Be(LoggingDefaults.MaskedValue);
    }

    [Test]
    public async Task Handle_ValidationResultFail_LogsTypeAndMessageOnly()
    {
        // Arrange
        var sut = CreateSut<ValidatedProbeQuery, Result<string>>();
        var error = new ValidationError
        {
            Message = "Validation failed",
            Errors = new Dictionary<string, string[]> { ["Email"] = ["jan.kowalski@example.com is not allowed"] }
        };

        // Act
        await sut.Handle(new ValidatedProbeQuery("jan.kowalski@example.com"), () => Task.FromResult(Result<string>.Fail(error)), CancellationToken.None);

        // Assert
        var failure = _loggerProvider.Logs.Should().ContainSingle(l => l.EventId.Id == OperationFailedWithResultEventId).Subject;
        failure.Properties.Select(p => p.Key).Should().BeEquivalentTo(
            "OperationName", "DurationMs", "ErrorType", "ErrorMessage", "{OriginalFormat}");
        Property(failure, "ErrorType").Should().Be(nameof(ValidationError));
        Property(failure, "ErrorMessage").Should().Be("Validation failed");
        failure.Properties.Should().NotContain(p => p.Value is Error || p.Value is Result
            || p.Value is IReadOnlyDictionary<string, string[]>);
        _loggerProvider.Logs.Should().NotContain(l => l.Message.Contains("jan.kowalski@example.com"));
    }

    private LoggingPipelineBehavior<TRequest, TResponse> CreateSut<TRequest, TResponse>() where TRequest : notnull
        => new(_loggerFactory.CreateLogger<LoggingPipelineBehavior<TRequest, TResponse>>());

    private Activity StoppedActivity(string operationName)
        => _stoppedActivities.Should().ContainSingle(a => a.OperationName == operationName).Subject;

    private static object? Property(RecordedLog log, string name)
        => log.Properties.Should().ContainSingle(p => p.Key == name).Subject.Value;
}
