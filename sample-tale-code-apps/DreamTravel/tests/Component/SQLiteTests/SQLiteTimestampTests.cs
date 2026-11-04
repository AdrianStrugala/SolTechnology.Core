using System.Globalization;
using DreamTravel.SQLite;
using FluentAssertions;
using NUnit.Framework;

namespace DreamTravel.FunctionalTests.SQLiteTests;

[TestFixture]
public class SQLiteTimestampTests
{
    private static readonly DateTimeOffset NoonUtc = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    private CultureInfo _culture = null!;

    [SetUp]
    public void SetUp() => _culture = CultureInfo.CurrentCulture;

    [TearDown]
    public void TearDown() => CultureInfo.CurrentCulture = _culture;

    [TestCase("th-TH")]
    [TestCase("ar-SA")]
    [TestCase("en-US")]
    public void Format_OffsetValueUnderAnyCulture_WritesTheUtcInstant(string culture)
    {
        // Arrange
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        var easternTime = NoonUtc.ToOffset(TimeSpan.FromHours(-5));

        // Act
        var text = SQLiteTimestamp.Format(easternTime);

        // Assert
        text.Should().Be("2026-11-03T12:00:00.0000000+00:00");
    }

    [TestCase("2026-11-03T12:00:00.0000000+00:00")]
    [TestCase("2026-11-03T07:00:00.0000000-05:00")]
    [TestCase("2026-11-03T12:00:00.0000000Z")]
    [TestCase("2026-11-03T12:00:00.0000000")]
    public void Parse_UnderNonGregorianCulture_ReturnsTheSameInstantInUtc(string text)
    {
        // Arrange
        CultureInfo.CurrentCulture = new CultureInfo("th-TH");

        // Act
        var parsed = SQLiteTimestamp.Parse(text);

        // Assert
        parsed.Should().Be(NoonUtc);
        parsed.Offset.Should().Be(TimeSpan.Zero);
    }
}
