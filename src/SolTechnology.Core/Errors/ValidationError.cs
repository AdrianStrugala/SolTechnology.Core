namespace SolTechnology.Core.Errors;

/// <summary>
/// Validation failure with per-field error messages. Maps to HTTP 400.
/// </summary>
public record ValidationError : Error
{
    /// <summary>
    /// Validation failures grouped by property name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; init; }
        = new Dictionary<string, string[]>(StringComparer.Ordinal);
}

