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
}
