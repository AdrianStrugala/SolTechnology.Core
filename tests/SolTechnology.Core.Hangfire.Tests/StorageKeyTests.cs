using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.Hangfire.Tests;

[TestFixture]
public class StorageKeyTests
{
    [Test]
    public void Fit_ValueAtLimit_IsKept()
    {
        // Arrange
        var value = new string('a', StorageKey.MaxRecurringJobIdLength);

        // Act
        var fitted = StorageKey.Fit(value, "readable", StorageKey.MaxRecurringJobIdLength);

        // Assert
        fitted.Should().Be(value);
    }

    [TestCase(87)]
    [TestCase(500)]
    public void Fit_LongValue_IsBoundedStableAndDistinct(int length)
    {
        // Arrange
        var value = new string('a', length);
        var other = new string('a', length - 1) + "b";

        // Act
        var fitted = StorageKey.Fit(value, new string('r', 200), StorageKey.MaxRecurringJobIdLength);

        // Assert
        fitted.Length.Should().Be(StorageKey.MaxRecurringJobIdLength);
        fitted.Should().Be(StorageKey.Fit(value, new string('r', 200), StorageKey.MaxRecurringJobIdLength));
        fitted.Should().NotBe(StorageKey.Fit(other, new string('r', 200), StorageKey.MaxRecurringJobIdLength));
        fitted.Should().MatchRegex("^r{73}-[0-9a-f]{12}$");
    }
}
