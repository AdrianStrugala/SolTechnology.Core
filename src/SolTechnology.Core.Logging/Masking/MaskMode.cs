namespace SolTechnology.Core.Logging.Masking;

/// <summary>
/// Controls how <see cref="PiiMask"/> renders a masked value.
/// </summary>
public enum MaskMode
{
    /// <summary>Replace the entire value with <see cref="LoggingDefaults.MaskedValue"/>.</summary>
    Full,

    /// <summary>Keep first and last N characters; replace the middle with <c>***</c>.</summary>
    Partial
}

