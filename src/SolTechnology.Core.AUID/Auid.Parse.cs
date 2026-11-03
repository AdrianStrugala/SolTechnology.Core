using System.Globalization;

namespace SolTechnology.Core.AUID;

public readonly partial struct Auid
{
    // Valid 3-letter codes (A-Z): 0 to 26^3 - 1
    private const long MaxValidCode = 26 * 26 * 26 - 1;

    /// <summary>
    /// Parses a string representation of an AUID.
    /// Expected format: COD_YYYYMMDDHHmmss_RANDOM (e.g., ORD_20241205123456_012345).
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">Format provider (not used, included for IParsable compliance).</param>
    /// <returns>The parsed AUID.</returns>
    /// <exception cref="FormatException">Thrown when string format is invalid.</exception>
    public static Auid Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
        {
            throw new FormatException($"Invalid Auid format: {s}");
        }

        return result;
    }

    /// <summary>
    /// Tries to parse a string representation of an AUID.
    /// Expected format: COD_YYYYMMDDHHmmss_RANDOM (e.g., CTY_20241205123456_012345).
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">Format provider (not used, included for IParsable compliance).</param>
    /// <param name="result">The parsed AUID if successful; otherwise, Empty.</param>
    /// <returns>True if parsing succeeded; false for a malformed string or a timestamp outside 2001-01-01 to 2137-02-07 06:28:15 UTC.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Auid result)
    {
        result = Empty;
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }

        ReadOnlySpan<char> span = s.AsSpan();
        if (span.Length != 25 || span[3] != Separator || span[18] != Separator)
        {
            return false;
        }

        if (!TryParseCode(span.Slice(0, 3), out long codeEncoded)
            || !TryParseSeconds(span.Slice(4, 14), out long timeVal)
            || !TryParseRandom(span.Slice(19, 6), out long randVal))
        {
            return false;
        }

        result = new Auid(Compose(codeEncoded, timeVal, randVal));
        return true;
    }

    /// <summary>
    /// Parses a long value into an AUID.
    /// Validates that the code part (first 15 bits) represents a valid 3-letter code (AAA-ZZZ).
    /// </summary>
    /// <param name="value">The long value to parse.</param>
    /// <returns>The parsed AUID.</returns>
    /// <exception cref="FormatException">Thrown when the code part exceeds valid range for 3-letter codes.</exception>
    public static Auid Parse(long value)
    {
        if (!TryParse(value, out var result))
        {
            throw new FormatException($"Invalid Auid value: {value}. The code part exceeds valid range for 3-letter codes (AAA-ZZZ).");
        }

        return result;
    }

    /// <summary>
    /// Tries to parse a long value into an AUID.
    /// Returns false if the code part (first 15 bits) doesn't represent a valid 3-letter code (AAA-ZZZ).
    /// </summary>
    /// <param name="value">The long value to parse.</param>
    /// <param name="result">The parsed AUID if successful; otherwise, Empty.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(long value, out Auid result)
    {
        result = Empty;

        long codePart = value >>> (BitsTime + BitsRandom);
        if (codePart < 0 || codePart > MaxValidCode)
        {
            return false;
        }

        result = new Auid(value);
        return true;
    }

    private static bool TryParseCode(ReadOnlySpan<char> code, out long codeEncoded)
    {
        codeEncoded = 0;
        for (int i = 0; i < 3; i++)
        {
            if (code[i] < 'A' || code[i] > 'Z')
            {
                return false;
            }
        }

        codeEncoded = Encode3Chars(code);
        return true;
    }

    private static bool TryParseSeconds(ReadOnlySpan<char> timestamp, out long seconds)
    {
        seconds = 0;
        if (!DateTime.TryParseExact(timestamp, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime dateTime))
        {
            return false;
        }

        seconds = (dateTime.Ticks - EpochTicks) / TimeSpan.TicksPerSecond;
        return seconds >= 0 && seconds <= MaskTime;
    }

    private static bool TryParseRandom(ReadOnlySpan<char> random, out long randomValue)
    {
        return long.TryParse(random, NumberStyles.None, null, out randomValue) && randomValue <= MaskRandom;
    }
}
