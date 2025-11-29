using System;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.AUID.Tests;

[TestFixture]
public class AuidParseStringTests
{
    [Test]
    public void Parse_WithValidString_ShouldCreateAuid()
    {
        // Arrange
        var original = Auid.New("TST");
        string serialized = original.ToString();

        // Act
        var parsed = Auid.Parse(serialized);

        // Assert
        parsed.Should().Be(original);
        parsed.Value.Should().Be(original.Value);
    }

    [Test]
    public void TryParse_WithValidString_ShouldReturnTrueAndCreateAuid()
    {
        // Arrange
        var original = Auid.New("ABC");
        string serialized = original.ToString();

        // Act
        var success = Auid.TryParse(serialized, null, out var parsed);

        // Assert
        success.Should().BeTrue();
        parsed.Should().Be(original);
    }

    [Test]
    public void Parse_WithNullString_ShouldThrowFormatException()
    {
        // Act
        Action act = () => Auid.Parse(null!);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithNullString_ShouldReturnFalse()
    {
        // Act
        var success = Auid.TryParse(null, null, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().Be(Auid.Empty);
    }

    [Test]
    public void Parse_WithEmptyString_ShouldThrowFormatException()
    {
        // Act
        Action act = () => Auid.Parse("");

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithEmptyString_ShouldReturnFalse()
    {
        // Act
        var success = Auid.TryParse("", null, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().Be(Auid.Empty);
    }

    [Test]
    public void Parse_WithWrongLength_ShouldThrowFormatException()
    {
        // Act
        Action act = () => Auid.Parse("ABC_12345");

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithWrongLength_ShouldReturnFalse()
    {
        // Act
        var success = Auid.TryParse("ABC_12345", null, out var result);

        // Assert
        success.Should().BeFalse();
    }

    [Test]
    public void Parse_WithWrongSeparator_ShouldThrowFormatException()
    {
        // Arrange - Using hyphen instead of underscore
        string invalid = "ABC-20241205123456-012345";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithWrongSeparator_ShouldReturnFalse()
    {
        // Arrange
        string invalid = "ABC-20241205123456-012345";

        // Act
        var success = Auid.TryParse(invalid, null, out var result);

        // Assert
        success.Should().BeFalse();
    }

    [Test]
    public void Parse_WithLowercaseCode_ShouldThrowFormatException()
    {
        // Arrange
        string invalid = "abc_20241205123456_012345";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithLowercaseCode_ShouldReturnFalse()
    {
        // Arrange
        string invalid = "abc_20241205123456_012345";

        // Act
        var success = Auid.TryParse(invalid, null, out var result);

        // Assert
        success.Should().BeFalse();
    }

    [Test]
    public void Parse_WithInvalidTimestamp_ShouldThrowFormatException()
    {
        // Arrange - Invalid date (month 13)
        string invalid = "ABC_20241305123456_012345";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithInvalidTimestamp_ShouldReturnFalse()
    {
        // Arrange - Invalid date (month 13)
        string invalid = "ABC_20241305123456_012345";

        // Act
        var success = Auid.TryParse(invalid, null, out var result);

        // Assert
        success.Should().BeFalse();
    }

    [Test]
    public void Parse_WithNonNumericRandom_ShouldThrowFormatException()
    {
        // Arrange
        string invalid = "ABC_20241205123456_ABCDEF";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithNonNumericRandom_ShouldReturnFalse()
    {
        // Arrange
        string invalid = "ABC_20241205123456_ABCDEF";

        // Act
        var success = Auid.TryParse(invalid, null, out var result);

        // Assert
        success.Should().BeFalse();
    }

    [Test]
    public void Parse_RoundTrip_ShouldPreserveValue()
    {
        // Arrange
        var original = Auid.New("RTT");

        // Act
        string serialized = original.ToString();
        var parsed = Auid.Parse(serialized);

        // Assert
        parsed.Should().Be(original);
        parsed.Value.Should().Be(original.Value);
        parsed.ToString().Should().Be(serialized);
    }

    [Test]
    public void TryParse_RoundTrip_ShouldPreserveValue()
    {
        // Arrange
        var original = Auid.New("RTT");
        string serialized = original.ToString();

        // Act
        var success = Auid.TryParse(serialized, null, out var parsed);

        // Assert
        success.Should().BeTrue();
        parsed.Should().Be(original);
        parsed.ToString().Should().Be(serialized);
    }

    [Test]
    public void Parse_WithValidFormat_AAA_ShouldWork()
    {
        // Arrange - January 1, 2001 at 00:00:01
        string valid = "AAA_20010101000001_000001";

        // Act
        var parsed = Auid.Parse(valid);

        // Assert
        parsed.Should().NotBe(Auid.Empty);
        parsed.ToString().Should().Be(valid);
    }

    [Test]
    public void Parse_WithValidFormat_ZZZ_ShouldWork()
    {
        // Arrange - December 31, 2136 at 23:59:59 (close to max date)
        string valid = "ZZZ_21361231235959_131071";

        // Act
        var parsed = Auid.Parse(valid);

        // Assert
        parsed.Should().NotBe(Auid.Empty);
        parsed.ToString().Should().Be(valid);
    }

    [Test]
    public void Parse_WithNonLetterInCode_ShouldThrowFormatException()
    {
        // Arrange
        string invalid = "A1C_20241205123456_012345";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_EmptyAuidString_ShouldParseCorrectly()
    {
        // Arrange
        string emptyString = Auid.Empty.ToString(); // "XXX_00010101000000_000000"

        // Act
        var success = Auid.TryParse(emptyString, null, out var parsed);

        // Assert - Empty.ToString() produces "XXX_..." which when parsed creates a non-zero Auid
        success.Should().BeTrue();
        parsed.ToString().Should().Be(emptyString);
        // Note: parsed.Value won't be 0 because "XXX" encodes to 16169, not 0
    }

    [Test]
    public void Parse_WithIFormatProvider_ShouldIgnoreProvider()
    {
        // Arrange
        var original = Auid.New("TST");
        string serialized = original.ToString();
        var provider = System.Globalization.CultureInfo.InvariantCulture;

        // Act
        var parsed = Auid.Parse(serialized, provider);

        // Assert
        parsed.Should().Be(original);
    }
}
