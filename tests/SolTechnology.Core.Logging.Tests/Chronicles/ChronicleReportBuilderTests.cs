using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleReportBuilderTests : ChronicleTestBase
{
    [TestCase(2, 2, false)]
    [TestCase(3, 1, true)]
    public void Report_IdenticalSteps_AggregatedFromThreshold(int steps, int expectedLines, bool isAggregated)
    {
        // Arrange
        var sut = CreateRecorder();
        using (sut.Begin("Root"))
        {
            for (var i = 0; i < steps; i++)
            {
                using var step = sut.Step("Db");
                Time.Advance(TimeSpan.FromMilliseconds(i + 1));
            }
        }

        // Act
        var report = ChronicleReportBuilder.Build(Listener.Completed.Single());

        // Assert
        var lines = report.Steps.Single().Children;
        lines.Should().HaveCount(expectedLines);
        lines[0].Count.Should().Be(isAggregated ? steps : 1);
        report.IsNPlusOneSuspected.Should().BeFalse();
        if (isAggregated)
        {
            lines[0].DurationMs.Should().Be(6);
            lines[0].MaxDurationMs.Should().Be(3);
        }
    }

    [Test]
    public void Report_FortyTwoIdenticalSQLSteps_AggregatesWithNPlusOneHint()
    {
        // Arrange
        var sut = CreateRecorder();
        var root = sut.Begin("GET /cities");
        var rootNode = AmbientChronicle.Current!;
        for (var i = 0; i < 42; i++)
        {
            AddClosedChild(rootNode, "SQL SELECT", startMs: i * 2, durationMs: 2, groupKey: "SQL SELECT#1");
        }

        AddClosedChild(rootNode, "SQL SELECT", startMs: 100, durationMs: 1, groupKey: "SQL SELECT#2");
        Time.Advance(TimeSpan.FromMilliseconds(101));
        root.Dispose();

        // Act
        var report = ChronicleReportBuilder.Build(Listener.Completed.Single());

        // Assert
        var lines = report.Steps.Single().Children;
        lines.Should().HaveCount(2);
        lines[0].Should().BeEquivalentTo(new { Name = "SQL SELECT", Count = 42, DurationMs = 84L, IsNPlusOneSuspected = true });
        lines[1].Count.Should().Be(1);
        report.IsNPlusOneSuspected.Should().BeTrue();
        report.ToString().Should().Contain("`- SQL SELECT [@100 ms]").And.Contain("|- SQL SELECT x42 [total 84 ms] [max 2 ms] [failed 0] N+1 suspected");
    }

    [Test]
    public void Report_OverflowAggregate_MergedIntoGroupLine()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxSteps = 5);
        using (sut.Begin("Root"))
        {
            for (var i = 0; i < 10; i++)
            {
                using var step = sut.Step("Db");
                Time.Advance(TimeSpan.FromMilliseconds(1));
            }
        }

        // Act
        var report = ChronicleReportBuilder.Build(Listener.Completed.Single());

        // Assert
        var line = report.Steps.Single().Children.Should().ContainSingle().Subject;
        line.Should().BeEquivalentTo(new { Name = "Db", Count = 10, DurationMs = 10L, IsNPlusOneSuspected = true });
        report.StepCount.Should().Be(5);
        report.DroppedStepCount.Should().Be(6);
    }

    [Test]
    public void Report_FailedChildUnderOkRoot_HasFailedSteps()
    {
        // Arrange
        var sut = CreateRecorder();
        using (sut.Begin("Root"))
        {
            for (var i = 0; i < 3; i++)
            {
                using var item = sut.Step("Item");
                using var nested = sut.Step("Nested");
                if (i == 1)
                {
                    nested.Fail("Boom");
                }
            }
        }

        // Act
        var report = ChronicleReportBuilder.Build(Listener.Completed.Single());

        // Assert
        report.Status.Should().Be(ChronicleStepStatus.Ok);
        report.HasFailedSteps.Should().BeTrue();
        report.Steps.Single().Children.Single().NestedCount.Should().Be(3);
    }

    [Test]
    public void Report_FailedOverflowStep_HasFailedSteps()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxSteps = 2);
        using (sut.Begin("Root"))
        {
            using (sut.Step("Recorded"))
            {
            }

            using var dropped = sut.Step("Dropped");
            dropped.Fail("Boom");
        }

        // Act
        var report = ChronicleReportBuilder.Build(Listener.Completed.Single());

        // Assert
        report.Status.Should().Be(ChronicleStepStatus.Ok);
        report.HasFailedSteps.Should().BeTrue();
        report.Steps.Single().Children.Should().Contain(s => s.Name == "Dropped" && s.FailedCount == 1 && s.Status == ChronicleStepStatus.Failed);
    }

    [Test]
    public void Report_OkTree_HasNoFailedSteps()
    {
        // Arrange
        var sut = CreateRecorder();
        using (sut.Begin("Root"))
        using (sut.Step("Db"))
        {
        }

        // Act
        var report = ChronicleReportBuilder.Build(Listener.Completed.Single());

        // Assert
        report.HasFailedSteps.Should().BeFalse();
    }
}
