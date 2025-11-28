using System;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.AUID.Tests;

[TestFixture]
public class AuidNewTests
{
    [Test]
    public void New_WithGenericType_ShouldGenerateCodeFromTypeName()
    {
        // Act
        var auid = Auid.New<Order>();

        // Assert
        auid.Should().NotBe(Auid.Empty);
        auid.Value.Should().NotBe(0);
        auid.ToString().Should().StartWith("ORD_");
    }

    [Test]
    public void New_WithExplicitValidCode_ShouldCreateAuid()
    {
        // Act
        var auid = Auid.New("USR");

        // Assert
        auid.Should().NotBe(Auid.Empty);
        auid.ToString().Should().StartWith("USR_");
    }

    [Test]
    public void New_WithValidCodeSpan_ShouldCreateAuid()
    {
        // Arrange
        ReadOnlySpan<char> code = "ABC".AsSpan();

        // Act
        var auid = Auid.New(code);

        // Assert
        auid.Should().NotBe(Auid.Empty);
        auid.ToString().Should().StartWith("ABC_");
    }

    [Test]
    public void New_WithLowercaseCode_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => Auid.New("abc");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*uppercase letters A-Z*");
    }

    [Test]
    public void New_WithMixedCaseCode_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => Auid.New("Abc");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*uppercase letters A-Z*");
    }

    [Test]
    public void New_WithNonLetterCode_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => Auid.New("AB1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*uppercase letters A-Z*");
    }

    [Test]
    public void New_WithTooShortCode_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => Auid.New("AB");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exactly 3 characters*");
    }

    [Test]
    public void New_WithTooLongCode_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => Auid.New("ABCD");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exactly 3 characters*");
    }

    [Test]
    public void New_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => Auid.New("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void New_WithNullString_ShouldInferFromCallerFile()
    {
        // Act
        var auid = Auid.New();

        // Assert - since this file is AuidNewTests.cs, it should extract from filename
        auid.Should().NotBe(Auid.Empty);
        // The code will be generated from "AuidNewTests" filename
    }

    [Test]
    public void New_CalledMultipleTimes_ShouldGenerateDifferentValues()
    {
        // Act
        var auid1 = Auid.New("TST");
        var auid2 = Auid.New("TST");
        var auid3 = Auid.New("TST");

        // Assert - due to timestamp and random components, they should be different
        auid1.Should().NotBe(auid2);
        auid2.Should().NotBe(auid3);
        auid1.Should().NotBe(auid3);
    }

    [Test]
    public void New_BoundaryAAA_ShouldCreateValidAuid()
    {
        // Act
        var auid = Auid.New("AAA");

        // Assert
        auid.Should().NotBe(Auid.Empty);
        auid.ToString().Should().StartWith("AAA_");
    }

    [Test]
    public void New_BoundaryZZZ_ShouldCreateValidAuid()
    {
        // Act
        var auid = Auid.New("ZZZ");

        // Assert
        auid.Should().NotBe(Auid.Empty);
        auid.ToString().Should().StartWith("ZZZ_");
    }

    [Test]
    public void New_WithSpanContainingLowercase_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            ReadOnlySpan<char> code = "abc".AsSpan();
            Auid.New(code);
        });
    }

    [Test]
    public void New_WithSpanWrongLength_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            ReadOnlySpan<char> code = "AB".AsSpan();
            Auid.New(code);
        });
    }
}

// Helper class for generic type testing
public class Order
{
}
