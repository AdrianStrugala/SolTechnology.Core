using System;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.AUID.Tests;

[TestFixture]
public class AuidParseLongTests
{
    [Test]
    public void Parse_WithValidLongValue_ShouldCreateAuid()
    {
        // Arrange
        var original = Auid.New("TST");
        long value = original.Value;

        // Act
        var parsed = Auid.Parse(value);

        // Assert
        parsed.Should().Be(original);
        parsed.Value.Should().Be(value);
    }

    [Test]
    public void TryParse_WithValidLongValue_ShouldReturnTrueAndCreateAuid()
    {
        // Arrange
        var original = Auid.New("ABC");
        long value = original.Value;

        // Act
        var success = Auid.TryParse(value, out var parsed);

        // Assert
        success.Should().BeTrue();
        parsed.Should().Be(original);
        parsed.Value.Should().Be(value);
    }

    [Test]
    public void Parse_WithZeroValue_ShouldCreateEmptyAuid()
    {
        // Act
        var parsed = Auid.Parse(0);

        // Assert
        parsed.Should().Be(Auid.Empty);
        parsed.Value.Should().Be(0);
    }

    [Test]
    public void TryParse_WithZeroValue_ShouldReturnTrueAndCreateEmptyAuid()
    {
        // Act
        var success = Auid.TryParse(0, out var parsed);

        // Assert
        success.Should().BeTrue();
        parsed.Should().Be(Auid.Empty);
    }

    [Test]
    public void Parse_WithInvalidCodePart_ShouldThrowFormatException()
    {
        // Arrange - Create a value where code part exceeds 17575 (max for ZZZ)
        // Code part is in the top 15 bits
        // 17576 is the first invalid code (one more than ZZZ = 17575)
        long invalidCode = 17576L << (32 + 17); // Shift to code position
        long invalidValue = invalidCode | (12345L << 17) | 1234L; // Add some time and random

        // Act
        Action act = () => Auid.Parse(invalidValue);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*code part exceeds valid range*");
    }

    [Test]
    public void TryParse_WithInvalidCodePart_ShouldReturnFalse()
    {
        // Arrange - Code part exceeds max valid value (17575)
        long invalidCode = 20000L << (32 + 17);
        long invalidValue = invalidCode | (12345L << 17) | 1234L;

        // Act
        var success = Auid.TryParse(invalidValue, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().Be(Auid.Empty);
    }

    [Test]
    public void Parse_WithMaxValidCode_ShouldSucceed()
    {
        // Arrange - 17575 is max valid code (represents ZZZ)
        long maxCode = 17575L << (32 + 17);
        long validValue = maxCode | (12345L << 17) | 1234L;

        // Act
        var parsed = Auid.Parse(validValue);

        // Assert
        parsed.Value.Should().Be(validValue);
        parsed.ToString().Should().StartWith("ZZZ_");
    }

    [Test]
    public void TryParse_WithMaxValidCode_ShouldReturnTrue()
    {
        // Arrange - 17575 represents ZZZ
        long maxCode = 17575L << (32 + 17);
        long validValue = maxCode | (12345L << 17) | 1234L;

        // Act
        var success = Auid.TryParse(validValue, out var parsed);

        // Assert
        success.Should().BeTrue();
        parsed.Value.Should().Be(validValue);
        parsed.ToString().Should().StartWith("ZZZ_");
    }

    [Test]
    public void Parse_WithMinValidCode_ShouldSucceed()
    {
        // Arrange - 0 is min valid code (represents AAA)
        long minCode = 0L << (32 + 17);
        long validValue = minCode | (12345L << 17) | 1234L;

        // Act
        var parsed = Auid.Parse(validValue);

        // Assert
        parsed.Value.Should().Be(validValue);
        parsed.ToString().Should().StartWith("AAA_");
    }

    [Test]
    public void TryParse_WithNegativeValueButValidCode_ShouldSucceed()
    {
        // Arrange - Negative values are valid if code part is within range
        // Create a value where the code part is valid (e.g., 100) but overall value is negative
        long validCode = 100L << (32 + 17);
        long negativeValue = validCode | (1L << 63); // Set sign bit

        // Act
        var success = Auid.TryParse(negativeValue, out var result);

        // Assert - Should succeed because code part is valid
        success.Should().BeTrue();
        result.Value.Should().Be(negativeValue);
    }

    [Test]
    public void Parse_RoundTrip_ShouldPreserveValue()
    {
        // Arrange
        var original = Auid.New("XYZ");

        // Act - Convert to long and back
        long value = original.Value;
        var roundTripped = Auid.Parse(value);

        // Assert
        roundTripped.Should().Be(original);
        roundTripped.Value.Should().Be(original.Value);
        roundTripped.ToString().Should().Be(original.ToString());
    }

    [Test]
    public void TryParse_RoundTrip_ShouldPreserveValue()
    {
        // Arrange
        var original = Auid.New("RTT");

        // Act
        var success = Auid.TryParse(original.Value, out var roundTripped);

        // Assert
        success.Should().BeTrue();
        roundTripped.Should().Be(original);
        roundTripped.ToString().Should().Be(original.ToString());
    }

    [Test]
    public void Parse_WithBoundaryCodeJustAboveMax_ShouldThrow()
    {
        // Arrange - 17576 is just above max valid (17575)
        long invalidCode = 17576L << (32 + 17);
        long invalidValue = invalidCode | (12345L << 17) | 1234L;

        // Act
        Action act = () => Auid.Parse(invalidValue);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void ImplicitConversion_ToLong_ShouldWork()
    {
        // Arrange
        var auid = Auid.New("CVT");

        // Act
        long value = auid;

        // Assert
        value.Should().Be(auid.Value);
    }

    [Test]
    public void ExplicitConversion_FromLong_ShouldWork()
    {
        // Arrange
        var original = Auid.New("CVT");
        long value = original.Value;

        // Act
        var converted = (Auid)value;

        // Assert
        converted.Should().Be(original);
    }
}
