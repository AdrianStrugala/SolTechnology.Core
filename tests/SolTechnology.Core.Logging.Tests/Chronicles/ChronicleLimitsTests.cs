using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleLimitsTests : ChronicleTestBase
{
    [Test]
    public void MaxSteps_Exceeded_DropsNodesAndAggregatesOverflow()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxSteps = 10);

        // Act
        using (sut.Begin("Root"))
        {
            for (var i = 0; i < 50; i++)
            {
                using var step = sut.Step("Db");
                Time.Advance(TimeSpan.FromMilliseconds(i == 49 ? 9 : 1));
                if (i >= 47)
                {
                    step.Fail("Timeout");
                }
            }

            using (sut.Step("Overflowing"))
            using (sut.Step("NestedUnderOverflow"))
            {
                AmbientChronicle.Current!.Name.Should().Be("Root");
            }
        }

        // Assert
        var root = Listener.Completed.Single();
        root.Node.SnapshotChildren().Should().HaveCount(9).And.OnlyContain(c => c.Name == "Db");
        root.StepCount.Should().Be(10);
        var overflow = root.Node.SnapshotOverflow().ToDictionary(o => o.Key, o => o.Value);
        overflow.Keys.Should().BeEquivalentTo("Db", "Overflowing", "NestedUnderOverflow");
        overflow["Db"].Count.Should().Be(41);
        overflow["Db"].FailedCount.Should().Be(3);
        overflow["Db"].MaxTicks.Should().Be(Ticks(9));
        root.DroppedStepCount.Should().Be(43);
        AmbientChronicle.Current.Should().BeNull();
    }

    [Test]
    public void MaxSteps_Exceeded_TenThousandDistinctNames_OverflowGroupsBounded()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxSteps = 10);

        // Act
        using (sut.Begin("Root"))
        {
            for (var i = 0; i < 10_000; i++)
            {
                using var step = sut.Step($"Step-{i}");
            }
        }

        // Assert
        var overflow = Listener.Completed.Single().Node.SnapshotOverflow();
        overflow.Should().HaveCountLessThanOrEqualTo(ChronicleOverflowGroups.MaxGroups + 1);
        overflow.Should().Contain(o => o.Key == ChronicleOverflowGroups.OtherKey);
        overflow.Sum(o => o.Value.Count).Should().Be(10_000 - 9);
    }

    [Test]
    public void MaxDepth_Exceeded_CountsDeeperSteps()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxDepth = 2);

        // Act
        using (sut.Begin("Root"))
        using (sut.Step("Level1"))
        using (sut.Step("Level2"))
        using (var tooDeep = sut.Step("Level3"))
        using (sut.Step("Level4"))
        {
            tooDeep.IsRecording.Should().BeFalse();
        }

        // Assert
        var root = Listener.Completed.Single();
        var level2 = NodeOf(root, "Level1", "Level2");
        level2.SnapshotChildren().Should().BeEmpty();
        level2.DroppedDeeperCount.Should().Be(2);
        root.StepCount.Should().Be(3);
    }

    [Test]
    public void MaxAttributesPerStep_Exceeded_IgnoresAndCounts()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxAttributesPerStep = 2);

        // Act
        using (var root = sut.Begin("Root"))
        {
            root.Set("A", 1).Set("B", 2).Set("C", 3).Set("D", 4).Set("A", 5);
        }

        // Assert
        var node = Listener.Completed.Single().Node;
        node.SnapshotAttributes().Should().Equal(new KeyValuePair<string, string>("A", "5"), new KeyValuePair<string, string>("B", "2"));
        node.DroppedAttributeCount.Should().Be(2);
    }

    [Test]
    public void AttributeValue_TooLong_TruncatedWithMarker()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxAttributeValueLength = 16);

        // Act
        using (var root = sut.Begin("Root"))
        {
            root.Set("Key", new string('v', 40)).Set("Short", "fits");
        }

        // Assert
        var attributes = Listener.Completed.Single().Node.SnapshotAttributes().ToDictionary(a => a.Key, a => a.Value);
        attributes["Key"].Should().Be(new string('v', 16) + "[truncated]");
        attributes["Short"].Should().Be("fits");
    }

    [Test]
    public void FailMessage_TooLong_TruncatedWithMarker()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxAttributeValueLength = 16);

        // Act
        using (var root = sut.Begin("Root"))
        {
            root.Fail(new InvalidOperationException(new string('m', 100)));
        }

        // Assert
        Listener.Completed.Single().Node.ErrorMessage.Should().Be(new string('m', 16) + "[truncated]");
    }

    [Test]
    public void StepName_TooLong_TruncatedWithMarker()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxAttributeValueLength = 16);

        // Act
        using (sut.Begin(new string('r', 30)))
        using (sut.Step(new string('s', 30)))
        {
        }

        // Assert
        var root = Listener.Completed.Single();
        root.Node.Name.Should().Be(new string('r', 16) + "[truncated]");
        var child = root.Node.SnapshotChildren().Single();
        child.Name.Should().Be(new string('s', 16) + "[truncated]");
        child.GroupKey.Should().Be(child.Name);
    }

    private long Ticks(int milliseconds) => (long)(milliseconds * Time.TimestampFrequency / 1000.0);
}
