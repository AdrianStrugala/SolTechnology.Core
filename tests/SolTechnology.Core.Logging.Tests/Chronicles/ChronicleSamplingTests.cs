using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleSamplingTests : ChronicleReportingTestBase
{
    [Test]
    public void SampleRate0_FastOk_NotEmitted()
    {
        // Arrange
        var sut = CreateReportingRecorder(o => o.SampleRate = 0);

        // Act
        using (sut.Begin("Fast"))
        using (sut.Step("Db"))
        {
        }

        // Assert
        Logs.GetSnapshot().Should().BeEmpty();
    }

    [Test]
    public void SampleRate0_Failed_Emitted()
    {
        // Arrange
        var sut = CreateReportingRecorder(o => o.SampleRate = 0);

        // Act
        using (var root = sut.Begin("Failing"))
        {
            root.Fail("Validation", "City is required");
        }

        // Assert
        Logs.GetSnapshot().Should().ContainSingle().Which.Level.Should().Be(LogLevel.Error);
    }

    [Test]
    public void SampleRate0_Slow_Emitted()
    {
        // Arrange
        var sut = CreateReportingRecorder(o => (o.SampleRate, o.SlowThreshold) = (0, TimeSpan.FromMilliseconds(500)));

        // Act
        using (sut.Begin("Slow"))
        {
            Time.Advance(TimeSpan.FromMilliseconds(500));
        }

        // Assert
        Logs.GetSnapshot().Should().ContainSingle().Which.Level.Should().Be(LogLevel.Warning);
    }

    [Test]
    public void SampleRate0_NPlusOne_Emitted()
    {
        // Arrange
        var sut = CreateReportingRecorder(o => o.SampleRate = 0);

        // Act
        using (sut.Begin("Loop"))
        {
            for (var i = 0; i < 10; i++)
            {
                using var step = sut.Step("SQL SELECT");
            }
        }

        // Assert
        var entry = Logs.GetSnapshot().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain("N+1 suspected");
    }

    [Test]
    public void SampleRate0_OkRootWithFailedChild_Emitted()
    {
        // Arrange
        var sut = CreateReportingRecorder(o => o.SampleRate = 0);

        // Act
        using (sut.Begin("PublishEvent"))
        using (var handler = sut.Step("Handler"))
        {
            handler.Fail(new InvalidOperationException("isolated"));
        }

        // Assert
        var entry = Logs.GetSnapshot().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain("[Ok]").And.Contain("Error=[InvalidOperationException: isolated]");
    }

    [Test]
    public void SampleRate0_OkRootWithFailedOverflowStep_Emitted()
    {
        // Arrange
        var sut = CreateReportingRecorder(o => (o.SampleRate, o.MaxSteps) = (0, 1));

        // Act
        using (sut.Begin("Import"))
        using (var dropped = sut.Step("Item"))
        {
            dropped.Fail("Boom");
        }

        // Assert
        Logs.GetSnapshot().Should().ContainSingle().Which.Message.Should().Contain("Item x1").And.Contain("[failed 1]");
    }
}
