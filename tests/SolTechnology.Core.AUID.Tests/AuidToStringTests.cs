using System;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.AUID.Tests;

[TestFixture]
public class AuidToStringTests
{
    [Test]
    public void ToString_ShouldHaveCorrectLength()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act
        string result = auid.ToString();

        // Assert
        result.Should().HaveLength(25);
    }

    [Test]
    public void ToString_ShouldStartWithCode()
    {
        // Arrange
        var auid = Auid.New("ABC");

        // Act
        string result = auid.ToString();

        // Assert
        result.Should().StartWith("ABC_");
    }

    [Test]
    public void ToString_ShouldUseUnderscoreSeparator()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act
        string result = auid.ToString();

        // Assert
        result[3].Should().Be('_');
        result[18].Should().Be('_');
    }

    [Test]
    public void ToString_ShouldHaveCorrectFormat()
    {
        // Arrange
        var auid = Auid.New("XYZ");

        // Act
        string result = auid.ToString();

        // Assert
        // Format: XXX_YYYYMMDDHHmmss_RRRRRR
        result.Should().MatchRegex(@"^[A-Z]{3}_\d{14}_\d{6}$");
    }

    [Test]
    public void ToString_EmptyAuid_ShouldReturnExpectedFormat()
    {
        // Act
        string result = Auid.Empty.ToString();

        // Assert
        result.Should().Be("XXX_00010101000000_000000");
    }

    [Test]
    public void ToString_MultipleCallsOnSameAuid_ShouldReturnSameString()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act
        string result1 = auid.ToString();
        string result2 = auid.ToString();
        string result3 = auid.ToString();

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
    }

    [Test]
    public void ToString_DifferentAuids_ShouldReturnDifferentStrings()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.New("TST");

        // Act
        string result1 = auid1.ToString();
        string result2 = auid2.ToString();

        // Assert
        result1.Should().NotBe(result2);
    }

    [Test]
    public void ToString_WithBoundaryCode_AAA_ShouldFormatCorrectly()
    {
        // Arrange
        var auid = Auid.New("AAA");

        // Act
        string result = auid.ToString();

        // Assert
        result.Should().StartWith("AAA_");
        result.Should().HaveLength(25);
    }

    [Test]
    public void ToString_WithBoundaryCode_ZZZ_ShouldFormatCorrectly()
    {
        // Arrange
        var auid = Auid.New("ZZZ");

        // Act
        string result = auid.ToString();

        // Assert
        result.Should().StartWith("ZZZ_");
        result.Should().HaveLength(25);
    }

    [Test]
    public void ToString_TimestampPart_ShouldBe14DateTimeCharacters()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act
        string result = auid.ToString();
        string timestampPart = result.Substring(4, 14);

        // Assert
        timestampPart.Should().MatchRegex(@"^\d{14}$");
        // Validate it's a valid date/time
        DateTime.TryParseExact(timestampPart, "yyyyMMddHHmmss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _).Should().BeTrue();
    }

    [Test]
    public void ToString_RandomPart_ShouldBe6DecimalDigits()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act
        string result = auid.ToString();
        string randomPart = result.Substring(19, 6);

        // Assert
        randomPart.Should().MatchRegex(@"^\d{6}$");
        int randomValue = int.Parse(randomPart);
        randomValue.Should().BeInRange(0, 131071); // 2^17 - 1
    }

    [Test]
    public void ToString_CodePart_ShouldBeUppercase()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act
        string result = auid.ToString();
        string codePart = result.Substring(0, 3);

        // Assert
        codePart.Should().MatchRegex("^[A-Z]{3}$");
    }

    [Test]
    public void ToString_CreatedInSequence_ShouldHaveSimilarTimestamp()
    {
        // Arrange & Act
        var auid1 = Auid.New("TST");
        var auid2 = Auid.New("TST");

        string result1 = auid1.ToString();
        string result2 = auid2.ToString();

        // Extract timestamp parts (YYYYMMDDHHmmss)
        string timestamp1 = result1.Substring(4, 14);
        string timestamp2 = result2.Substring(4, 14);

        // Assert - timestamps should be the same or very close (might differ by 1 second)
        var time1 = DateTime.ParseExact(timestamp1, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var time2 = DateTime.ParseExact(timestamp2, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        Math.Abs((time1 - time2).TotalSeconds).Should().BeLessThan(2);
    }

    [Test]
    public void ToString_RoundTrip_WithParse_ShouldBeIdentical()
    {
        // Arrange
        var original = Auid.New("RTT");
        string originalString = original.ToString();

        // Act
        var parsed = Auid.Parse(originalString);
        string parsedString = parsed.ToString();

        // Assert
        parsedString.Should().Be(originalString);
    }

    [Test]
    public void ToString_ShouldNotContainSpaces()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act
        string result = auid.ToString();

        // Assert
        result.Should().NotContain(" ");
    }

    [Test]
    public void ToString_ShouldNotContainHyphens()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act
        string result = auid.ToString();

        // Assert
        result.Should().NotContain("-");
    }
}
