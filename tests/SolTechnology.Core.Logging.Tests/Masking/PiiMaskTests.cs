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
        PiiMask.Partial("1234567890", 3).Should().Be("123***890");
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
        PiiMask.Apply("user@example.com", MaskMode.Partial, 4).Should().Be("user***.com");
    }
}

