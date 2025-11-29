using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// A unique 64-bit identifier (AUID) designed for high-performance scenarios.
/// Structure: [Code: 15 bits] [Timestamp: 32 bits] [Random: 17 bits].
/// Total: 64 bits used.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[TypeConverter(typeof(AuidTypeConverter))]
public readonly struct Auid : IComparable<Auid>, IEquatable<Auid>, IParsable<Auid>
{
    // --- Configuration ---
    // Total 64 bits:
    // Code:   15 bits (17,576 combinations) -> Bits 63-49
    // Time:   32 bits (Seconds since 2001)  -> Bits 48-17
    // Random: 17 bits (131,072 combinations)-> Bits 16-0
    
    private const int BitsRandom = 17;
    private const int BitsTime = 32;
    // BitsCode = 15 (Calculated implicitly)

    private const long MaskRandom = (1L << BitsRandom) - 1;     // 0x1FFFF
    private const long MaskTime = (1L << BitsTime) - 1;         // 0xFFFFFFFF
    
    // Epoch: 2001-01-01 UTC
    private static readonly long EpochTicks = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
    
    // Defaults
    private const string DefaultCode = "XXX";
    private const char Separator = '_';

    /// <summary>
    /// The raw 64-bit integer value. Can be negative due to full 64-bit usage.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Represents an empty AUID with value 0.
    /// </summary>
    public static readonly Auid Empty = new Auid(0);

    private Auid(long value) => Value = value;

    // --- Factory Methods ---

    /// <summary>
    /// Creates a new AUID based on the type name (e.g., Order -> ORD).
    /// Zero allocation.
    /// </summary>
    /// <typeparam name="T">The type to derive the 3-letter code from.</typeparam>
    /// <returns>A new AUID with code derived from type name.</returns>
    /// <example>
    /// <code>
    /// var orderId = Auid.New&lt;Order&gt;();
    /// Console.WriteLine(orderId); // Output: ORD_2B1A3F12_1A2B3
    /// </code>
    /// </example>
    /// <exception cref="InvalidOperationException">Thrown when current date exceeds year 2137 (timestamp overflow).</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Auid New<T>()
    {
        return CreateInternal(typeof(T).Name.AsSpan(), generateCode: true);
    }

    /// <summary>
    /// Creates a new AUID.
    /// If code is provided, uses that 3-letter code (must be exactly 3 uppercase letters A-Z).
    /// If code is null, infers the code from the source file name using [CallerFilePath].
    /// </summary>
    /// <param name="code">Optional 3-letter code (must be uppercase A-Z). If null, uses caller file name.</param>
    /// <param name="callerFilePath">Automatically populated by compiler. Do not pass manually.</param>
    /// <returns>A new AUID with specified or inferred code.</returns>
    /// <example>
    /// <code>
    /// // Explicit code
    /// var userId = Auid.New("USR");
    ///
    /// // Inferred from file name (e.g., OrderService.cs -> ORS)
    /// var id = Auid.New();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when code is not exactly 3 characters or contains non-uppercase letters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when current date exceeds year 2137 (timestamp overflow).</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Auid New(string? code = null, [CallerFilePath] string callerFilePath = "")
    {
        if (code != null)
        {
            // Explicit code provided - use it directly without processing
            if (code.Length != 3)
                throw new ArgumentException("Code must be exactly 3 characters.", nameof(code));

            // Validate uppercase letters A-Z
            for (int i = 0; i < 3; i++)
            {
                if (code[i] < 'A' || code[i] > 'Z')
                    throw new ArgumentException("Code must contain only uppercase letters A-Z.", nameof(code));
            }

            return CreateInternal(code.AsSpan(), generateCode: false);
        }
        else
        {
            // Infer from caller file path
            ReadOnlySpan<char> span = callerFilePath.AsSpan();
            if (span.IsEmpty) return CreateInternal(DefaultCode.AsSpan(), generateCode: false);

            // Manual "GetFileNameWithoutExtension" on Span to avoid allocation
            int lastSeparator = span.LastIndexOfAny('/', '\\');
            int lastDot = span.LastIndexOf('.');

            int start = lastSeparator + 1;
            int length = (lastDot > start) ? (lastDot - start) : (span.Length - start);

            return CreateInternal(span.Slice(start, length), generateCode: true);
        }
    }

    /// <summary>
    /// Creates a new AUID from a specific 3-letter code (must be exactly 3 uppercase letters A-Z).
    /// Zero allocation version using ReadOnlySpan.
    /// </summary>
    /// <param name="code">3-letter code span (must be uppercase A-Z).</param>
    /// <returns>A new AUID with the specified code.</returns>
    /// <example>
    /// <code>
    /// ReadOnlySpan&lt;char&gt; code = "PRD".AsSpan();
    /// var productId = Auid.New(code);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when code is not exactly 3 characters or contains non-uppercase letters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when current date exceeds year 2137 (timestamp overflow).</exception>
    public static Auid New(ReadOnlySpan<char> code)
    {
        if (code.Length != 3)
            throw new ArgumentException("Code must be exactly 3 characters.");

        // Validate uppercase letters A-Z
        for (int i = 0; i < 3; i++)
        {
            if (code[i] < 'A' || code[i] > 'Z')
                throw new ArgumentException("Code must contain only uppercase letters A-Z.");
        }

        return CreateInternal(code, generateCode: false);
    }

    // --- Core Logic (Zero Alloc) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Auid CreateInternal(ReadOnlySpan<char> input, bool generateCode)
    {
        // 1. Encode Code (15 bits)
        long codeEncoded;

        if (generateCode)
        {
            // Generate code from input name (e.g., "Order" -> "ORD")
            Span<char> codeBuffer = stackalloc char[3];
            GenerateCode3(input, codeBuffer);
            codeEncoded = Encode3Chars(codeBuffer);
        }
        else
        {
            // Use input directly as 3-letter code (already validated)
            codeEncoded = Encode3Chars(input);
        }

        // 2. Encode Time (32 bits)
        long seconds = (DateTime.UtcNow.Ticks - EpochTicks) / TimeSpan.TicksPerSecond;

        // Validate timestamp doesn't exceed 32-bit limit
        if (seconds > MaskTime)
        {
            throw new InvalidOperationException(
                "AUID timestamp overflow: Przekroczono rok 2137! Papież Polak robi BUM i AUID już nie działa. Czas na nową epokę, albo niech ktoś w końcu przepisze ten system na 64-bitowy timestamp!");
        }

        long timeEncoded = seconds & MaskTime;

        // 3. Encode Random (17 bits)
        long randomEncoded = Random.Shared.Next() & MaskRandom;

        // Combine: [CODE 15] [TIME 32] [RND 17]
        long finalValue = (codeEncoded << (BitsTime + BitsRandom))
                        | (timeEncoded << BitsRandom)
                        | randomEncoded;

        return new Auid(finalValue);
    }

    /// <summary>
    /// Generates "ORD" from "Order", "USR" from "User" without allocations.
    /// </summary>
    private static void GenerateCode3(ReadOnlySpan<char> input, Span<char> output)
    {
        if (input.IsEmpty)
        {
            DefaultCode.AsSpan().CopyTo(output);
            return;
        }

        int outIdx = 0;

        // 1. First char (always taken)
        char first = input[0];
        // Fast ToUpper
        if (first >= 'a' && first <= 'z') first = (char)(first - 32);
        if (first < 'A' || first > 'Z') first = 'X';
        output[outIdx++] = first;

        // 2. Next consonants
        for (int i = 1; i < input.Length && outIdx < 3; i++)
        {
            char c = input[i];
            // Fast ToUpper
            if (c >= 'a' && c <= 'z') c = (char)(c - 32);

            // IsLetter check (A-Z only)
            if (c >= 'A' && c <= 'Z')
            {
                // IsVowel check
                if (!(c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U' || c == 'Y'))
                {
                    output[outIdx++] = c;
                }
            }
        }

        // 3. Padding
        while (outIdx < 3)
        {
            output[outIdx++] = 'X';
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Encode3Chars(ReadOnlySpan<char> code)
    {
        // A=0, B=1...
        return ((long)(code[0] - 'A') * 676) +
               ((long)(code[1] - 'A') * 26) +
               ((long)(code[2] - 'A'));
    }

    // --- Formatting & Parsing ---

    /// <summary>
    /// Format: COD_YYYYMMDDHHmmss_RANDOM
    /// Length: 3 + 1 + 14 + 1 + 6 = 25 chars
    /// Timestamp: YYYYMMDDHHmmss (readable date/time)
    /// Random: 6 decimal digits (17 bits = 0-131071)
    /// Example: CTY_20241205123456_012345
    /// </summary>
    public override string ToString()
    {
        if (Value == 0) return $"{DefaultCode}{Separator}00010101000000{Separator}000000";

        return string.Create(25, Value, (span, val) =>
        {
            // Decode
            long codePart = val >>> (BitsTime + BitsRandom); // Use ulong shift to handle sign bit correctly
            long timePart = (val >> BitsRandom) & MaskTime;
            long randPart = val & MaskRandom;

            // Write Code
            span[0] = (char)('A' + (codePart / 676));
            span[1] = (char)('A' + ((codePart % 676) / 26));
            span[2] = (char)('A' + (codePart % 26));

            span[3] = Separator;

            // Write Time (YYYYMMDDHHmmss - 14 chars)
            // Convert seconds since epoch to DateTime
            var dateTime = new DateTime(EpochTicks + (timePart * TimeSpan.TicksPerSecond), DateTimeKind.Utc);
            dateTime.TryFormat(span.Slice(4, 14), out _, "yyyyMMddHHmmss");

            span[18] = Separator;

            // Write Random (6 decimal digits, padded with zeros)
            randPart.TryFormat(span.Slice(19, 6), out _, "D6");
        });
    }

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
            throw new FormatException($"Invalid Auid format: {s}");
        return result;
    }

    /// <summary>
    /// Tries to parse a string representation of an AUID.
    /// Expected format: COD_YYYYMMDDHHmmss_RANDOM (e.g., CTY_20241205123456_012345).
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">Format provider (not used, included for IParsable compliance).</param>
    /// <param name="result">The parsed AUID if successful; otherwise, Empty.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Auid result)
    {
        result = Empty;
        if (string.IsNullOrEmpty(s)) return false;

        ReadOnlySpan<char> span = s.AsSpan();
        // Format: AAA_YYYYMMDDHHmmss_RRRRRR (25 chars)
        if (span.Length != 25) return false;
        if (span[3] != Separator || span[18] != Separator) return false;

        // 1. Parse Code
        var c0 = span[0]; var c1 = span[1]; var c2 = span[2];
        // Simplified manual check to avoid LINQ/Calls
        if (c0 < 'A' || c0 > 'Z' || c1 < 'A' || c1 > 'Z' || c2 < 'A' || c2 > 'Z') return false;

        long codeEncoded = ((long)(c0 - 'A') * 676) +
                           ((long)(c1 - 'A') * 26) +
                           ((long)(c2 - 'A'));

        // 2. Parse Time (YYYYMMDDHHmmss - 14 chars)
        if (!DateTime.TryParseExact(span.Slice(4, 14), "yyyyMMddHHmmss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out DateTime dateTime))
            return false;

        // Convert DateTime to seconds since epoch
        long timeVal = (dateTime.Ticks - EpochTicks) / TimeSpan.TicksPerSecond;

        // 3. Parse Random (6 decimal digits)
        if (!long.TryParse(span.Slice(19, 6), System.Globalization.NumberStyles.None, null, out long randVal))
            return false;

        // Validate random is within 17-bit range
        if (randVal > MaskRandom)
            return false;

        // Reconstruct
        long finalValue = (codeEncoded << (BitsTime + BitsRandom))
                        | (timeVal << BitsRandom)
                        | randVal;

        result = new Auid(finalValue);
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
            throw new FormatException($"Invalid Auid value: {value}. The code part exceeds valid range for 3-letter codes (AAA-ZZZ).");
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

        // Extract code part (top 15 bits)
        long codePart = value >>> (BitsTime + BitsRandom);

        // Valid 3-letter codes (A-Z): 0 to 26^3 - 1 = 17575
        const long MaxValidCode = 26 * 26 * 26 - 1; // 17575

        if (codePart < 0 || codePart > MaxValidCode)
            return false;

        result = new Auid(value);
        return true;
    }

    // --- Standard Boilerplate ---

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Auid other && Equals(other);

    /// <inheritdoc />
    public bool Equals(Auid other) => Value == other.Value;

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(Auid other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Determines whether two AUID instances are equal.
    /// </summary>
    public static bool operator ==(Auid left, Auid right) => left.Value == right.Value;

    /// <summary>
    /// Determines whether two AUID instances are not equal.
    /// </summary>
    public static bool operator !=(Auid left, Auid right) => left.Value != right.Value;

    /// <summary>
    /// Implicitly converts an AUID to its raw 64-bit long value.
    /// </summary>
    public static implicit operator long(Auid a) => a.Value;

    /// <summary>
    /// Explicitly converts a 64-bit long value to an AUID.
    /// </summary>
    public static explicit operator Auid(long v) => new Auid(v);
}

/// <summary>
/// Type converter for AUID, enabling automatic conversion in JSON serializers and other frameworks.
/// Converts between AUID and string representations.
/// </summary>
public class AuidTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        if (value is string str) return Auid.Parse(str);
        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Auid auid) return auid.ToString();
        return base.ConvertTo(context, culture, value, destinationType);
    }
}