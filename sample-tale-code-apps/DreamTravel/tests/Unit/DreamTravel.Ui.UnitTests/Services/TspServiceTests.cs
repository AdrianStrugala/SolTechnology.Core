using DreamTravel.Ui.Services;
using FluentAssertions;
using NUnit.Framework;

namespace DreamTravel.Ui.UnitTests.Services;

public class TspServiceTests
{
    [Test]
    public void FormatTimeFromSeconds_FormatsZeroSeconds_Correctly()
    {
        // Act
        var result = TspService.FormatTimeFromSeconds(0);

        // Assert
        result.Should().Be("0:00:00");
    }

    [Test]
    public void FormatTimeFromSeconds_FormatsOneHour_Correctly()
    {
        // Act
        var result = TspService.FormatTimeFromSeconds(3600);

        // Assert
        result.Should().Be("1:00:00");
    }

    [Test]
    public void FormatTimeFromSeconds_FormatsHoursMinutesSeconds_Correctly()
    {
        // Arrange - 2 hours, 30 minutes, 45 seconds
        var totalSeconds = (2 * 3600) + (30 * 60) + 45;

        // Act
        var result = TspService.FormatTimeFromSeconds(totalSeconds);

        // Assert
        result.Should().Be("2:30:45");
    }

    [Test]
    public void FormatTimeFromSeconds_FormatsLessThanOneHour_Correctly()
    {
        // Arrange - 45 minutes, 30 seconds
        var totalSeconds = (45 * 60) + 30;

        // Act
        var result = TspService.FormatTimeFromSeconds(totalSeconds);

        // Assert
        result.Should().Be("0:45:30");
    }

    [Test]
    public void FormatTimeFromSeconds_FormatsLargeValues_Correctly()
    {
        // Arrange - 25 hours, 15 minutes, 5 seconds
        var totalSeconds = (25 * 3600) + (15 * 60) + 5;

        // Act
        var result = TspService.FormatTimeFromSeconds(totalSeconds);

        // Assert
        result.Should().Be("25:15:05");
    }

    [Test]
    public void FormatTimeFromSeconds_PadsMinutesAndSeconds_WithLeadingZero()
    {
        // Arrange - 1 hour, 5 minutes, 3 seconds
        var totalSeconds = 3600 + (5 * 60) + 3;

        // Act
        var result = TspService.FormatTimeFromSeconds(totalSeconds);

        // Assert
        result.Should().Be("1:05:03");
    }
}
