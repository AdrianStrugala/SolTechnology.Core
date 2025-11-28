using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.AUID.Tests;

[TestFixture]
public class AuidEqualityTests
{
    [Test]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.Parse(auid1.Value);

        // Act & Assert
        auid1.Equals(auid2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.New("TST");

        // Act & Assert
        auid1.Equals(auid2).Should().BeFalse();
    }

    [Test]
    public void Equals_WithObject_SameValue_ShouldReturnTrue()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        object auid2 = Auid.Parse(auid1.Value);

        // Act & Assert
        auid1.Equals(auid2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithObject_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var auid = Auid.New("TST");
        object other = "not an auid";

        // Act & Assert
        auid.Equals(other).Should().BeFalse();
    }

    [Test]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act & Assert
        auid.Equals(null).Should().BeFalse();
    }

    [Test]
    public void OperatorEquals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.Parse(auid1.Value);

        // Act & Assert
        (auid1 == auid2).Should().BeTrue();
    }

    [Test]
    public void OperatorEquals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.New("TST");

        // Act & Assert
        (auid1 == auid2).Should().BeFalse();
    }

    [Test]
    public void OperatorNotEquals_WithSameValue_ShouldReturnFalse()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.Parse(auid1.Value);

        // Act & Assert
        (auid1 != auid2).Should().BeFalse();
    }

    [Test]
    public void OperatorNotEquals_WithDifferentValue_ShouldReturnTrue()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.New("TST");

        // Act & Assert
        (auid1 != auid2).Should().BeTrue();
    }

    [Test]
    public void GetHashCode_WithSameValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.Parse(auid1.Value);

        // Act
        var hash1 = auid1.GetHashCode();
        var hash2 = auid2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Test]
    public void GetHashCode_WithDifferentValue_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.New("TST");

        // Act
        var hash1 = auid1.GetHashCode();
        var hash2 = auid2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Test]
    public void CompareTo_WithSameValue_ShouldReturnZero()
    {
        // Arrange
        var auid1 = Auid.New("TST");
        var auid2 = Auid.Parse(auid1.Value);

        // Act
        var result = auid1.CompareTo(auid2);

        // Assert
        result.Should().Be(0);
    }

    [Test]
    public void CompareTo_WithSmallerValue_ShouldReturnPositive()
    {
        // Arrange
        var auid1 = Auid.Parse(1000);
        var auid2 = Auid.Parse(500);

        // Act
        var result = auid1.CompareTo(auid2);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Test]
    public void CompareTo_WithLargerValue_ShouldReturnNegative()
    {
        // Arrange
        var auid1 = Auid.Parse(500);
        var auid2 = Auid.Parse(1000);

        // Act
        var result = auid1.CompareTo(auid2);

        // Assert
        result.Should().BeLessThan(0);
    }

    [Test]
    public void Empty_ShouldBeEqualToZeroValue()
    {
        // Arrange
        var zero = Auid.Parse(0);

        // Act & Assert
        Auid.Empty.Should().Be(zero);
        Auid.Empty.Value.Should().Be(0);
    }

    [Test]
    public void Empty_MultipleCalls_ShouldReturnSameInstance()
    {
        // Act
        var empty1 = Auid.Empty;
        var empty2 = Auid.Empty;

        // Assert
        empty1.Should().Be(empty2);
        empty1.Value.Should().Be(empty2.Value);
    }

    [Test]
    public void StructEquality_ByValue_ShouldWork()
    {
        // Arrange
        var auid = Auid.New("TST");
        var copy = auid; // Struct copy

        // Act & Assert
        auid.Should().Be(copy);
        auid.Value.Should().Be(copy.Value);
    }

    [Test]
    public void Dictionary_ShouldUseAsKey()
    {
        // Arrange
        var dict = new Dictionary<Auid, string>();
        var auid1 = Auid.New("TST");
        var auid2 = Auid.Parse(auid1.Value);

        // Act
        dict[auid1] = "test";

        // Assert
        dict.Should().ContainKey(auid2);
        dict[auid2].Should().Be("test");
    }

    [Test]
    public void HashSet_ShouldNotContainDuplicates()
    {
        // Arrange
        var set = new HashSet<Auid>();
        var auid = Auid.New("TST");

        // Act
        var added1 = set.Add(auid);
        var added2 = set.Add(auid);

        // Assert
        added1.Should().BeTrue();
        added2.Should().BeFalse();
        set.Should().HaveCount(1);
    }

    [Test]
    public void Equals_EmptyWithEmpty_ShouldReturnTrue()
    {
        // Arrange
        var empty1 = Auid.Empty;
        var empty2 = Auid.Empty;

        // Act & Assert
        empty1.Equals(empty2).Should().BeTrue();
        (empty1 == empty2).Should().BeTrue();
    }

    [Test]
    public void Equals_EmptyWithNonEmpty_ShouldReturnFalse()
    {
        // Arrange
        var auid = Auid.New("TST");

        // Act & Assert
        Auid.Empty.Equals(auid).Should().BeFalse();
        (Auid.Empty == auid).Should().BeFalse();
    }
}
