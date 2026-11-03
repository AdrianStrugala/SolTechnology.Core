using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Masking;

namespace SolTechnology.Core.Logging.Tests.Masking;

[TestFixture]
public class PiiMaskTests
{
    [Test]
    public void Full_ReturnsDefaultMaskedValue()
    {
        PiiMask.Full("secret").Should().Be(LoggingDefaults.MaskedValue);
    }

    [Test]
    public void Full_NullInput_ReturnsDefaultMaskedValue()
    {
        PiiMask.Full(null).Should().Be(LoggingDefaults.MaskedValue);
    }

    [Test]
    public void Partial_LongEnoughInput_KeepsFirstAndLastChars()
    {
        PiiMask.Partial("1234567890123", 3).Should().Be("123***123");
    }

    [Test]
    public void Partial_ShortInput_FallsToFull()
    {
        PiiMask.Partial("ab", 3).Should().Be(LoggingDefaults.MaskedValue);
    }

    [Test]
    public void Partial_ExactlyTwiceKeepChars_FallsToFull()
    {
        PiiMask.Partial("abcdef", 3).Should().Be(LoggingDefaults.MaskedValue);
    }

    [TestCase("1234567", 3)]
    [TestCase("123456789012", 3)]
    [TestCase("abcd", 1)]
    public void Partial_ShortValue_FullyMasked(string value, int keepChars)
    {
        // Act
        var masked = PiiMask.Partial(value, keepChars);

        // Assert
        masked.Should().Be(LoggingDefaults.MaskedValue);
    }

    [Test]
    public void Partial_NegativeKeep_Throws()
    {
        // Act
        var act = () => PiiMask.Partial("1234567890", -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("keepChars");
    }

    [Test]
    public void Partial_NullInput_FallsToFull()
    {
        PiiMask.Partial(null, 3).Should().Be(LoggingDefaults.MaskedValue);
    }

    [Test]
    public void Partial_EmptyInput_FallsToFull()
    {
        PiiMask.Partial("", 3).Should().Be(LoggingDefaults.MaskedValue);
    }

    [Test]
    public void Apply_FullMode_DelegatesToFull()
    {
        PiiMask.Apply("secret", MaskMode.Full).Should().Be(LoggingDefaults.MaskedValue);
    }

    [Test]
    public void Apply_PartialMode_DelegatesToPartial()
    {
        PiiMask.Apply("john.doe@example.com", MaskMode.Partial, 4).Should().Be("john***.com");
    }
}

