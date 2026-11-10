using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Masking;
using SolTechnology.Core.Logging.Operations;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleActivityTests : ChronicleTestBase
{
    private readonly ConcurrentQueue<Activity> _stopped = new();
    private ActivityListener? _listener;

    [TearDown]
    public void DisposeListener()
    {
        _listener?.Dispose();
        _listener = null;
        _stopped.Clear();
    }

    [Test]
    public void Step_WithListener_StartsActivityWithParentChain()
    {
        // Arrange
        Listen();
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("ImportCities"))
        using (sut.Step("Db"))
        {
        }

        // Assert
        var db = _stopped.Single(a => a.OperationName == "Db");
        var root = _stopped.Single(a => a.OperationName == "ImportCities");
        db.Parent.Should().BeSameAs(root);
        db.Kind.Should().Be(ActivityKind.Internal);
        Activity.Current.Should().BeNull();
    }

    [Test]
    public void Step_WithoutListener_StartsNoActivity()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("ImportCities"))
        using (sut.Step("Db"))
        {
            // Assert
            Activity.Current.Should().BeNull();
        }
    }

    [Test]
    public void Step_ChronicleDisabledWithListener_StillStartsActivity()
    {
        // Arrange
        Listen();
        var sut = CreateRecorder(o => o.Enabled = false);

        // Act
        using (var step = sut.Step("Handler"))
        {
            step.IsRecording.Should().BeFalse();
        }

        // Assert
        _stopped.Should().ContainSingle(a => a.OperationName == "Handler");
        Listener.Completed.Should().BeEmpty();
    }

    [Test]
    public void EmitActivitiesFalse_StartsNoActivity()
    {
        // Arrange
        Listen();
        var sut = CreateRecorder(o => o.EmitActivities = false);

        // Act
        using (sut.Begin("ImportCities"))
        using (sut.Step("Db"))
        {
        }

        // Assert
        _stopped.Should().BeEmpty();
        Listener.Completed.Should().ContainSingle();
    }

    [Test]
    public void SetMasked_TagHoldsMaskedValueOnly()
    {
        // Arrange
        Listen();
        var sut = CreateRecorder();

        // Act
        using (var root = sut.Begin("Register"))
        {
            root.SetMasked("Email", "jan.kowalski@example.com", MaskMode.Partial, keepChars: 2).Set("Attempt", 2);
        }

        // Assert
        var activity = _stopped.Single();
        activity.GetTagItem("Email").Should().Be("ja***om");
        activity.GetTagItem("Attempt").Should().Be("2");
    }

    [Test]
    public void Fail_SetsErrorStatusAndException()
    {
        // Arrange
        Listen();
        var sut = CreateRecorder();

        // Act
        using (var root = sut.Begin("Import"))
        {
            root.Fail(new TimeoutException("db timed out"));
        }

        // Assert
        var activity = _stopped.Single();
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("db timed out");
        activity.GetTagItem("error.type").Should().Be(nameof(TimeoutException));
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
    }

    [Test]
    public void Cancel_TagsCancelledWithUnsetStatus()
    {
        // Arrange
        Listen();
        var sut = CreateRecorder();

        // Act
        using (var root = sut.Begin("Import"))
        {
            root.Cancel();
        }

        // Assert
        var activity = _stopped.Single();
        activity.Status.Should().Be(ActivityStatusCode.Unset);
        activity.GetTagItem("chronicle.status").Should().Be("cancelled");
    }

    [Test]
    public void Report_CarriesTraceIdOfAmbientActivity()
    {
        // Arrange
        var sut = CreateRecorder();
        using var ambient = new Activity("Inbound").SetIdFormat(ActivityIdFormat.W3C).Start();

        // Act
        using (sut.Begin("Import"))
        {
        }

        // Assert
        ChronicleReportBuilder.Build(Listener.Completed.Single()).TraceId.Should().Be(ambient.TraceId.ToHexString());
    }

    [Test]
    public void MaxSteps_Exceeded_StartsNoActivityForDroppedSteps()
    {
        // Arrange
        Listen();
        var sut = CreateRecorder(o => (o.MaxSteps, o.MaxDepth) = (10, 1));

        // Act
        using (sut.Begin("Import"))
        {
            for (var i = 0; i < 50; i++)
            {
                using var step = sut.Step("Db");
                using var tooDeep = sut.Step("TooDeep");
            }
        }

        // Assert
        _stopped.Should().HaveCount(10);
        _stopped.Should().NotContain(a => a.OperationName == "TooDeep");
    }

    private void Listen()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CoreLoggingActivitySources.OperationsName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _stopped.Enqueue
        };
        ActivitySource.AddActivityListener(_listener);
    }
}
