using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleRecorderTests : ChronicleTestBase
{
    [Test]
    public void Begin_NoAmbientRoot_StartsRoot()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (var root = sut.Begin("ImportCities"))
        {
            // Assert
            root.IsRecording.Should().BeTrue();
            sut.IsActive.Should().BeTrue();
            AmbientChronicle.Current!.Parent.Should().BeNull();
            AmbientChronicle.Current.Name.Should().Be("ImportCities");
        }

        Listener.Completed.Should().ContainSingle().Which.Node.Name.Should().Be("ImportCities");
        sut.IsActive.Should().BeFalse();
    }

    [Test]
    public void Begin_InsideOpenRoot_StartsChild()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("Request"))
        using (sut.Begin("Command"))
        {
        }

        // Assert
        var root = Listener.Completed.Should().ContainSingle().Subject;
        NodeOf(root, "Command").Parent.Should().BeSameAs(root.Node);
    }

    [Test]
    public void Begin_AfterRootClosed_StartsNewRoot()
    {
        // Arrange
        var sut = CreateRecorder();
        var first = sut.Begin("First");
        first.Dispose();
        AmbientChronicle.Current = Listener.Completed[0].Node;

        // Act
        using (sut.Begin("Second"))
        {
        }

        // Assert
        Listener.Completed.Select(r => r.Node.Name).Should().Equal("First", "Second");
        Listener.Completed[1].Node.Parent.Should().BeNull();
    }

    [Test]
    public void Step_NoRoot_ReturnsNonRecordingHandle()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using var step = sut.Step("Orphan");

        // Assert
        step.IsRecording.Should().BeFalse();
        AmbientChronicle.Current.Should().BeNull();
    }

    [Test]
    public void Step_AfterRootClosed_ReturnsNonRecordingHandle()
    {
        // Arrange
        var sut = CreateRecorder();
        sut.Begin("Root").Dispose();
        var closedRoot = Listener.Completed[0];
        AmbientChronicle.Current = closedRoot.Node;

        // Act
        using var step = sut.Step("TooLate");

        // Assert
        step.IsRecording.Should().BeFalse();
        closedRoot.Node.SnapshotChildren().Should().BeEmpty();
        closedRoot.LateStepCount.Should().Be(0);
    }

    [Test]
    public void Step_UnderClosedParentInOpenRoot_RecordedAndCountedLate()
    {
        // Arrange
        var sut = CreateRecorder();
        using var root = sut.Begin("Root");
        var parent = sut.Step("Parent");
        var parentNode = AmbientChronicle.Current!;
        parent.Dispose();
        AmbientChronicle.Current = parentNode;

        // Act
        using (var late = sut.Step("Continuation"))
        {
            late.IsRecording.Should().BeTrue();
        }

        // Assert
        parentNode.SnapshotChildren().Should().ContainSingle(c => c.Name == "Continuation");
        parentNode.Root.LateStepCount.Should().Be(1);
    }

    [Test]
    public void Current_Dispose_DoesNotCloseNode()
    {
        // Arrange
        var sut = CreateRecorder();
        using var root = sut.Begin("Root");
        using var step = sut.Step("Step");
        var stepNode = AmbientChronicle.Current!;

        // Act
        sut.Current.Set("Key", 1).Dispose();

        // Assert
        stepNode.IsClosed.Should().BeFalse();
        AmbientChronicle.Current.Should().BeSameAs(stepNode);
        stepNode.SnapshotAttributes().Should().ContainSingle(a => a.Key == "Key" && a.Value == "1");
    }

    [Test]
    public void Durations_UseTimeProvider()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("Root"))
        {
            Time.Advance(TimeSpan.FromMilliseconds(10));
            using (sut.Step("Db"))
            {
                Time.Advance(TimeSpan.FromMilliseconds(42));
            }
        }

        // Assert
        var root = Listener.Completed.Single();
        var db = NodeOf(root, "Db");
        Time.GetElapsedTime(db.StartTimestamp, db.EndTimestamp).Should().Be(TimeSpan.FromMilliseconds(42));
        Time.GetElapsedTime(root.Node.StartTimestamp, db.StartTimestamp).Should().Be(TimeSpan.FromMilliseconds(10));
        root.StartedAt.Should().Be(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
    }
}
