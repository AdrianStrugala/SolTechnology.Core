using System.Buffers;

namespace SolTechnology.Core.Logging.Correlations;

/// <summary>
/// Accepts an inbound <c>X-Correlation-Id</c> only when it is at most <see cref="CorrelationId.MaxLength"/>
/// characters from <c>[A-Za-z0-9._:-]</c>, so the value is safe to echo and to stamp into log scopes.
/// </summary>
internal static class CorrelationIdValidator
{
    private static readonly SearchValues<char> AllowedChars =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789._:-");

    /// <summary>Returns <c>null</c> for an acceptable value, otherwise a reason that never contains the value.</summary>
    public static string? Validate(string value)
    {
        if (value.Length > CorrelationId.MaxLength)
        {
            return $"{CorrelationId.HeaderKey} must not exceed {CorrelationId.MaxLength} characters.";
        }

        if (value.AsSpan().ContainsAnyExcept(AllowedChars))
        {
            return $"{CorrelationId.HeaderKey} may contain only letters, digits, '.', '_', ':' and '-'.";
        }

        return null;
    }
}
