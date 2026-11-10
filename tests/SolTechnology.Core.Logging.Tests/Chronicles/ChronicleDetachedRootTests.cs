using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleDetachedRootTests : ChronicleTestBase
{
    [Test]
    public void BeginRoot_NoAmbientRoot_StartsRoot()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (var root = sut.BeginRoot("ImportCities"))
        {
            root.IsRecording.Should().BeTrue();
        }

        // Assert
        var completed = Listener.Completed.Should().ContainSingle().Subject;
        completed.Node.Name.Should().Be("ImportCities");
        completed.DetachedFrom.Should().BeNull();
    }

    [Test]
    public void BeginRoot_InsideOpenRoot_StartsSeparateRoot()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("Request"))
        {
            using (sut.BeginRoot("Event"))
            using (sut.Step("Handler"))
            {
            }
        }

        // Assert
        Listener.Completed.Select(r => r.Node.Name).Should().Equal("Event", "Request");
        Listener.Completed[1].Node.SnapshotChildren().Should().BeEmpty();
        NodeOf(Listener.Completed[0], "Handler").Should().NotBeNull();
    }

    [Test]
    public void BeginRoot_InsideOpenRoot_SetsDetachedFromAttribute()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("GET /cities"))
        using (sut.BeginRoot("Event CityImported"))
        {
        }

        // Assert
        var report = ChronicleReportBuilder.Build(Listener.Completed[0]);
        report.Steps.Single().Attributes.Should().Contain("DetachedFrom", "GET /cities");
        report.ToString().Should().StartWith("\nEvent CityImported [@0 ms] [0 ms] [ok] DetachedFrom=GET /cities");
    }

    [Test]
    public void BeginRoot_Dispose_RestoresPreviousAmbientNode()
    {
        // Arrange
        var sut = CreateRecorder();
        using var request = sut.Begin("Request");
        using var handler = sut.Step("Handler");
        var handlerNode = AmbientChronicle.Current!;

        // Act
        sut.BeginRoot("Event").Dispose();
        using (sut.Step("AfterEvent"))
        {
        }

        // Assert
        AmbientChronicle.Current.Should().BeSameAs(handlerNode);
        handlerNode.SnapshotChildren().Should().ContainSingle(c => c.Name == "AfterEvent");
    }

    [Test]
    public async Task BeginRoot_InTaskRunOutlivingCaller_ReportsOwnRootAndNoLateStepInCaller()
    {
        // Arrange
        var sut = CreateRecorder();
        var callerDone = new TaskCompletionSource();
        Task background;

        // Act
        using (sut.Begin("Request"))
        {
            background = Task.Run(async () =>
            {
                await callerDone.Task;
                using var root = sut.BeginRoot("Event");
                using var step = sut.Step("Handler");
            });
        }

        callerDone.SetResult();
        await background;

        // Assert
        var request = Listener.Completed.Single(r => r.Node.Name == "Request");
        var detached = Listener.Completed.Single(r => r.Node.Name == "Event");
        request.LateStepCount.Should().Be(0);
        request.Node.SnapshotChildren().Should().BeEmpty();
        NodeOf(detached, "Handler").IsUnfinished.Should().BeFalse();
        detached.DetachedFrom.Should().BeNull("the caller's root had closed before the event started");
    }

    [Test]
    public void BeginRoot_ChronicleDisabled_ReturnsNonRecordingHandle()
    {
        // Arrange
        var sut = CreateRecorder(o => o.Enabled = false);

        // Act
        using (var root = sut.BeginRoot("Event"))
        {
            // Assert
            root.IsRecording.Should().BeFalse();
            AmbientChronicle.Current.Should().BeNull();
        }

        Listener.Completed.Should().BeEmpty();
    }

    [Test]
    public void BeginRoot_WithListener_ActivityParentIsAmbientActivity()
    {
        // Arrange
        var stopped = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CoreLoggingActivitySources.OperationsName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Enqueue
        };
        ActivitySource.AddActivityListener(listener);
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("Request"))
        using (sut.Step("Publish"))
        using (sut.BeginRoot("Event"))
        {
        }

        // Assert
        var publish = stopped.Single(a => a.OperationName == "Publish");
        var detached = stopped.Single(a => a.OperationName == "Event");
        detached.Parent.Should().BeSameAs(publish);
        ChronicleReportBuilder.Build(Listener.Completed.Single(r => r.Node.Name == "Event")).TraceId.Should().Be(publish.TraceId.ToHexString());
    }
}
