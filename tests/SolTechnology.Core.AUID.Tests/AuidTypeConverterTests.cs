using System;
using System.ComponentModel;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.AUID.Tests;

[TestFixture]
public class AuidTypeConverterTests
{
    private readonly TypeConverter _sut = TypeDescriptor.GetConverter(typeof(Auid));

    [Test]
    public void TypeConverter_RoundTrip()
    {
        // Arrange
        var original = Auid.Parse("ORD_20241205123456_012345");

        // Act
        var text = _sut.ConvertToInvariantString(original);
        var converted = _sut.ConvertFromInvariantString(text!);

        // Assert
        _sut.Should().BeOfType<AuidTypeConverter>();
        text.Should().Be("ORD_20241205123456_012345");
        converted.Should().Be(original);
    }

    [Test]
    public void ConvertFrom_Invalid_ThrowsFormatExceptionWithValue()
    {
        // Act
        var act = () => _sut.ConvertFromInvariantString("not-an-auid");

        // Assert
        act.Should().Throw<FormatException>().WithMessage("*not-an-auid*");
    }
}
