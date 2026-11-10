using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Masking;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleStepTests : ChronicleTestBase
{
    [Test]
    public void Dispose_OutOfOrder_KeepsAmbientNodeConsistent()
    {
        // Arrange
        var sut = CreateRecorder();
        using var root = sut.Begin("Root");
        var outer = sut.Step("Outer");
        var outerNode = AmbientChronicle.Current!;
        var inner = sut.Step("Inner");
        var innerNode = AmbientChronicle.Current!;

        // Act
        outer.Dispose();
        var afterOuter = AmbientChronicle.Current;
        inner.Dispose();

        // Assert
        afterOuter.Should().BeSameAs(innerNode, "the ambient node is restored only by the handle that owns it");
        AmbientChronicle.Current.Should().BeSameAs(outerNode);
        outerNode.IsClosed.Should().BeTrue();
        innerNode.IsClosed.Should().BeTrue();
    }

    [Test]
    public void Dispose_ListenerThrows_DoesNotThrow()
    {
        // Arrange
        var sut = new ChronicleRecorder(Options.Create(new ChronicleOptions()), Time, [new ThrowingRootListener(), Listener]);
        var root = sut.Begin("Root");

        // Act
        var act = () => root.Dispose();

        // Assert
        act.Should().NotThrow();
        Listener.Completed.Should().ContainSingle();
    }

    [Test]
    public void Dispose_Twice_ClosesOnce()
    {
        // Arrange
        var sut = CreateRecorder();
        var root = sut.Begin("Root");
        var step = sut.Step("Step");
        var stepNode = AmbientChronicle.Current!;
        Time.Advance(TimeSpan.FromMilliseconds(5));
        step.Dispose();
        var firstEnd = stepNode.EndTimestamp;
        Time.Advance(TimeSpan.FromMilliseconds(5));

        // Act
        step.Dispose();
        root.Dispose();
        root.Dispose();

        // Assert
        stepNode.EndTimestamp.Should().Be(firstEnd);
        Listener.Completed.Should().ContainSingle();
    }

    [Test]
    public void Dispose_RootWithOpenChild_MarksChildUnfinished()
    {
        // Arrange
        var sut = CreateRecorder();
        var root = sut.Begin("Root");
        _ = sut.Step("NeverClosed");
        AmbientChronicle.Current = AmbientChronicle.Current!.Parent;
        Time.Advance(TimeSpan.FromMilliseconds(7));

        // Act
        root.Dispose();

        // Assert
        var completed = Listener.Completed.Single();
        var child = NodeOf(completed, "NeverClosed");
        child.IsUnfinished.Should().BeTrue();
        child.EndTimestamp.Should().Be(completed.Node.EndTimestamp);
        completed.Node.IsUnfinished.Should().BeFalse();
    }

    [Test]
    public void Fail_Exception_SetsFailedWithTypeAndMessage()
    {
        // Arrange
        var sut = CreateRecorder();
        var exception = new TimeoutException("db timed out");

        // Act
        using (var root = sut.Begin("Root"))
        {
            using (var step = sut.Step("Db"))
            {
                step.Fail(exception);
            }

            root.Fail(exception);
        }

        // Assert
        var completed = Listener.Completed.Single();
        var db = NodeOf(completed, "Db");
        db.Status.Should().Be(ChronicleStepStatus.Failed);
        db.ErrorType.Should().Be(nameof(TimeoutException));
        db.ErrorMessage.Should().Be("db timed out");
        completed.Exception.Should().BeSameAs(exception);
    }

    [Test]
    public void Cancel_SetsCancelled()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (var root = sut.Begin("Root"))
        {
            root.Cancel();
        }

        // Assert
        Listener.Completed.Single().Node.Status.Should().Be(ChronicleStepStatus.Cancelled);
    }

    [Test]
    public void SetMasked_StoresMaskedValueOnly()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (var root = sut.Begin("Root"))
        {
            root.SetMasked("Email", "jan.kowalski@example.com", MaskMode.Partial, keepChars: 2)
                .SetMasked("Pesel", "90010112345");
        }

        // Assert
        var attributes = Listener.Completed.Single().Node.SnapshotAttributes().ToDictionary(a => a.Key, a => a.Value);
        attributes["Email"].Should().Be("ja***om");
        attributes["Pesel"].Should().Be(PiiMask.Full("90010112345"));
        attributes.Values.Should().NotContain(v => v.Contains("kowalski") || v.Contains("9001"));
    }
}
