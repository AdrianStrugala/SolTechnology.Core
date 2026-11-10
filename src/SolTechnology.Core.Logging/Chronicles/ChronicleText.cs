namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>Text bounds shared by step names, attribute values, and failure messages.</summary>
internal static class ChronicleText
{
    internal const string TruncatedMarker = "[truncated]";

    internal static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), TruncatedMarker);

    // A line break inside a name or value would split the rendered tree into unrelated lines.
    internal static string SingleLine(string? text)
        => text is null || text.AsSpan().IndexOfAny('\r', '\n', '\t') < 0 ? text ?? string.Empty : text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
}
