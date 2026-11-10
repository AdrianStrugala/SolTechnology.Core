namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Options for Chronicle, the per-operation step report of <c>SolTechnology.Core.Logging</c>.
/// Bound from the configuration section <see cref="SectionName"/>.
/// </summary>
public sealed class ChronicleOptions
{
    /// <summary>Configuration section the options bind to: <c>Logging:Core:Chronicle</c>.</summary>
    public const string SectionName = "Logging:Core:Chronicle";

    /// <summary>
    /// Records steps and emits reports when <c>true</c> (default). When <c>false</c>, every
    /// <see cref="IChronicle"/> call returns a non-recording <see cref="ChronicleStep"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum recorded steps per root, the root included (default 256, range 1–10000). Later
    /// steps are not recorded one by one; they are added to an aggregate line of their parent.
    /// </summary>
    public int MaxSteps { get; set; } = 256;

    /// <summary>
    /// Maximum depth below the root (default 12, range 1–64). Deeper steps are not recorded and
    /// are counted as <c>+{n} deeper steps</c> on their deepest recorded ancestor.
    /// </summary>
    public int MaxDepth { get; set; } = 12;

    /// <summary>
    /// Maximum attributes per step (default 8, range 0–64). Later keys are ignored and counted as
    /// <c>+{n} attributes dropped</c>; setting an existing key overwrites it.
    /// </summary>
    public int MaxAttributesPerStep { get; set; } = 8;

    /// <summary>
    /// Maximum length of an attribute value, a step name, and a failure message (default 200,
    /// range 16–4096). Longer text is cut and suffixed with <c>[truncated]</c>.
    /// </summary>
    public int MaxAttributeValueLength { get; set; } = 200;
}
