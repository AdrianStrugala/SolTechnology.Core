using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleEmissionPolicyTests
{
    private readonly ChronicleEmissionPolicy _sut = new(new ChronicleOptions());

    [TestCase(ChronicleStepStatus.Ok, 10, null, LogLevel.Information)]
    [TestCase(ChronicleStepStatus.Ok, 1_000, null, LogLevel.Warning)]
    [TestCase(ChronicleStepStatus.Cancelled, 10, null, LogLevel.Warning)]
    [TestCase(ChronicleStepStatus.Failed, 10, null, LogLevel.Error)]
    [TestCase(ChronicleStepStatus.Ok, 10, 404, LogLevel.Warning)]
    [TestCase(ChronicleStepStatus.Ok, 10, 500, LogLevel.Error)]
    public void LevelOf_OutcomeDurationAndHTTPStatus_EscalatesLevel(ChronicleStepStatus status, int durationMs, int? httpStatusCode, LogLevel expected)
    {
        // Act
        var level = _sut.LevelOf(status, TimeSpan.FromMilliseconds(durationMs), httpStatusCode);

        // Assert
        level.Should().Be(expected);
    }
}
