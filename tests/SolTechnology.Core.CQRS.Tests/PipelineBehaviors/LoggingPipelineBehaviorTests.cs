using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SolTechnology.Core.CQRS.PipelineBehaviors;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.CQRS.Tests.PipelineBehaviors;

[TestFixture]
public class LoggingPipelineBehaviorTests
{
    private sealed record ListenerProbeQuery(string Name);

    [Test]
    public async Task LoggingPipelineBehavior_ListenerOnOperationsName_ReceivesActivity()
    {
        // Arrange
        var stopped = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CoreLoggingActivitySources.OperationsName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add
        };
        ActivitySource.AddActivityListener(listener);
        var sut = new LoggingPipelineBehavior<ListenerProbeQuery, string>(
            NullLogger<LoggingPipelineBehavior<ListenerProbeQuery, string>>.Instance);

        // Act
        var response = await sut.Handle(new ListenerProbeQuery("Warsaw"), () => Task.FromResult("ok"), CancellationToken.None);

        // Assert
        response.Should().Be("ok");
        var activity = stopped.Should().ContainSingle(a => a.OperationName == nameof(ListenerProbeQuery)).Subject;
        activity.Source.Name.Should().Be(CoreLoggingActivitySources.OperationsName);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }
}
