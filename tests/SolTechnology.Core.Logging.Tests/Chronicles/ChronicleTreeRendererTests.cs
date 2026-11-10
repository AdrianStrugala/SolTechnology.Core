using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleTreeRendererTests : ChronicleTestBase
{
    [Test]
    public void Render_NestedTree_MatchesGolden()
    {
        // Arrange
        var sut = CreateRecorder();
        using (sut.Begin("GET /cities"))
        {
            Advance(2);
            using (var find = sut.Step("FindCity"))
            {
                find.Set("City", "Krakow").Set("Limit", 10);
                Advance(3);
                using (sut.Step("Db"))
                {
                    Advance(10);
                }

                Advance(1);
            }

            Advance(4);
            using (sut.Step("Http"))
            {
                Advance(5);
            }
        }

        // Act
        var rendered = Render();

        // Assert
        rendered.Should().Be("""

            GET /cities [@0 ms] [25 ms] [ok]
            |- FindCity [@2 ms] [14 ms] [ok] City=Krakow Limit=10
            |  `- Db [@5 ms] [10 ms] [ok]
            `- Http [@20 ms] [5 ms] [ok]
            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public void Render_ParallelSiblings_OrderedByOffset()
    {
        // Arrange
        var sut = CreateRecorder();
        var root = sut.Begin("Root");
        var rootNode = AmbientChronicle.Current!;
        AddClosedChild(rootNode, "Slow", startMs: 3, durationMs: 9);
        AddClosedChild(rootNode, "Fast", startMs: 1, durationMs: 1);
        Advance(12);
        root.Dispose();

        // Act
        var rendered = Render();

        // Assert
        rendered.Should().Be("""

            Root [@0 ms] [12 ms] [ok]
            |- Fast [@1 ms] [1 ms] [ok]
            `- Slow [@3 ms] [9 ms] [ok]
            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public void Render_FailedStep_ShowsErrorTypeAndMessage()
    {
        // Arrange
        var sut = CreateRecorder();
        using (var root = sut.Begin("Root"))
        {
            using (var db = sut.Step("Db"))
            {
                db.Fail(new TimeoutException("db\ntimed out"));
            }

            root.Fail("Validation");
        }

        // Act
        var rendered = Render();

        // Assert
        rendered.Should().Be("""

            Root [@0 ms] [0 ms] [failed] Error=[Validation]
            `- Db [@0 ms] [0 ms] [failed] Error=[TimeoutException: db timed out]
            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public void Render_UnfinishedAndLate_ShowMarkers()
    {
        // Arrange
        var sut = CreateRecorder(o => (o.MaxDepth, o.MaxAttributesPerStep) = (1, 1));
        var root = sut.Begin("Root");
        var rootNode = AmbientChronicle.Current!;
        var parent = sut.Step("Parent");
        var parentNode = AmbientChronicle.Current!;
        sut.Step("TooDeep").Dispose();
        parent.Set("A", 1).Set("B", 2).Dispose();
        AmbientChronicle.Current = parentNode;
        sut.Step("Late").Dispose();
        AmbientChronicle.Current = rootNode;
        Advance(1);
        _ = sut.Step("NeverClosed");
        Advance(4);
        root.Dispose();

        // Act
        var rendered = Render();

        // Assert
        rendered.Should().Be("""

            Root [@0 ms] [5 ms] [ok]
            |- Parent [@0 ms] [0 ms] [ok] A=1 +1 attributes dropped +2 deeper steps
            `- NeverClosed [@1 ms] [4 ms] [ok] (unfinished)
            +1 late steps
            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public void Render_WorstCaseDefaults_AtMost32768Chars()
    {
        // Arrange
        var sut = CreateRecorder();
        var value = new string('v', 200);
        using (var root = sut.Begin(new string('r', 1_000)))
        {
            SetEightAttributes(root, value);
            for (var i = 0; i < 300; i++)
            {
                using var step = sut.Step(i + new string('s', 1_000));
                SetEightAttributes(step, value);
            }
        }

        // Act
        var rendered = Render();

        // Assert
        rendered.Length.Should().BeLessThanOrEqualTo(32_768);
        rendered.Should().StartWith("\n" + new string('r', 200) + "[truncated] [@0 ms]");
        rendered.Should().MatchRegex(@"\n\+\d+ lines truncated$");
    }

    [Test]
    public void Render_OutputIsASCIIOnly()
    {
        // Arrange
        var sut = CreateRecorder(o => o.MaxSteps = 4);
        using (var root = sut.Begin("Root"))
        {
            root.Set("Key", "Value");
            for (var i = 0; i < 12; i++)
            {
                using var step = sut.Step("Db");
                using var nested = sut.Step("Nested");
                nested.Fail(new InvalidOperationException("boom"));
            }
        }

        // Act
        var rendered = Render();

        // Assert
        rendered.Should().Contain("x12").And.Contain("N+1 suspected").And.Contain("steps dropped");
        rendered.ToCharArray().Should().OnlyContain(c => c < 128);
    }

    private string Render() => ChronicleReportBuilder.Build(Listener.Completed.Single()).ToString();

    private void Advance(int milliseconds) => Time.Advance(TimeSpan.FromMilliseconds(milliseconds));

    private static void SetEightAttributes(ChronicleStep step, string value)
    {
        for (var a = 0; a < 8; a++)
        {
            step.Set($"Attribute{a}", value);
        }
    }
}
