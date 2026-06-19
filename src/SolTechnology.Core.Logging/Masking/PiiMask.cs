namespace SolTechnology.Core.Logging.Masking;

/// <summary>
/// Static helpers for masking PII values before they enter log scopes.
/// Guard-rail: never returns null or empty — always returns a meaningful masked token.
/// </summary>
public static class PiiMask
{
    /// <summary>Replaces the entire value with <see cref="LoggingDefaults.MaskedValue"/>.</summary>
    public static string Full(string? value) => LoggingDefaults.MaskedValue;

    /// <summary>
    /// Keeps the first <paramref name="keepChars"/> and last <paramref name="keepChars"/>
    /// characters; replaces the middle with <c>***</c>.
    /// Falls through to <see cref="Full"/> when the value is too short to partially mask.
    /// </summary>
    public static string Partial(string? value, int keepChars = 3)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 2 * keepChars)
            return Full(value);

        return string.Concat(
            value.AsSpan(0, keepChars),
            "***",
            value.AsSpan(value.Length - keepChars));
    }

    /// <summary>Applies the specified <paramref name="mode"/>.</summary>
    public static string Apply(string? value, MaskMode mode, int keepChars = 3)
        => mode switch
        {
            MaskMode.Full => Full(value),
            MaskMode.Partial => Partial(value, keepChars),
            _ => Full(value)
        };
}

