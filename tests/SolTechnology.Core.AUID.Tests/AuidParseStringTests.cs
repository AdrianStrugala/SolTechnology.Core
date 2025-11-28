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
        string invalid = "ABC-12345678-12345";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithWrongSeparator_ShouldReturnFalse()
    {
        // Arrange
        string invalid = "ABC-12345678-12345";

        // Act
        var success = Auid.TryParse(invalid, null, out var result);

        // Assert
        success.Should().BeFalse();
    }

    [Test]
    public void Parse_WithLowercaseCode_ShouldThrowFormatException()
    {
        // Arrange
        string invalid = "abc_12345678_12345";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithLowercaseCode_ShouldReturnFalse()
    {
        // Arrange
        string invalid = "abc_12345678_12345";

        // Act
        var success = Auid.TryParse(invalid, null, out var result);

        // Assert
        success.Should().BeFalse();
    }

    [Test]
    public void Parse_WithNonHexTimestamp_ShouldThrowFormatException()
    {
        // Arrange
        string invalid = "ABC_GGGGGGGG_12345";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithNonHexTimestamp_ShouldReturnFalse()
    {
        // Arrange
        string invalid = "ABC_GGGGGGGG_12345";

        // Act
        var success = Auid.TryParse(invalid, null, out var result);

        // Assert
        success.Should().BeFalse();
    }

    [Test]
    public void Parse_WithNonHexRandom_ShouldThrowFormatException()
    {
        // Arrange
        string invalid = "ABC_12345678_GGGGG";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_WithNonHexRandom_ShouldReturnFalse()
    {
        // Arrange
        string invalid = "ABC_12345678_GGGGG";

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
        // Arrange
        string valid = "AAA_00000001_00001";

        // Act
        var parsed = Auid.Parse(valid);

        // Assert
        parsed.Should().NotBe(Auid.Empty);
        parsed.ToString().Should().Be(valid);
    }

    [Test]
    public void Parse_WithValidFormat_ZZZ_ShouldWork()
    {
        // Arrange
        string valid = "ZZZ_FFFFFFFF_1FFFF";

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
        string invalid = "A1C_12345678_12345";

        // Act
        Action act = () => Auid.Parse(invalid);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void TryParse_EmptyAuidString_ShouldParseCorrectly()
    {
        // Arrange
        string emptyString = Auid.Empty.ToString(); // "XXX_00000000_00000"

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
