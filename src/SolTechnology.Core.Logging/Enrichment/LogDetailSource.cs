namespace SolTechnology.Core.Logging.Enrichment;

/// <summary>
/// Where to pull a per-request log property from.
/// </summary>
public enum LogDetailSource
{
    /// <summary>JSON request body (single property by name; case-insensitive PascalCase fallback).</summary>
    Body,

    /// <summary>HTTP request header.</summary>
    Header,

    /// <summary>Query string first, then route values (in that order).</summary>
    Url,
}

