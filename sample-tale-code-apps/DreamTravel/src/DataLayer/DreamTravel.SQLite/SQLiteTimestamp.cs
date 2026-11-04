using System.Globalization;

namespace DreamTravel.SQLite;

/// <summary>
/// Writes and reads an instant as round-trip ISO 8601 text in UTC (<c>2026-11-03T12:05:00.0000000+00:00</c>),
/// independent of the host's time zone and culture. Text without an offset is read as UTC.
/// </summary>
public static class SQLiteTimestamp
{
    public static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    public static DateTimeOffset Parse(string text) =>
        DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
}
