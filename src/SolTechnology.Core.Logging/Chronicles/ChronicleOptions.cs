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

    /// <summary>
    /// Siblings with the same name (for SQL steps: the same statement) collapse into one
    /// aggregated line once there are this many of them (default 3, at least 2).
    /// </summary>
    public int AggregateThreshold { get; set; } = 3;

    /// <summary>
    /// An aggregated line with at least this many steps is marked <c>N+1 suspected</c>
    /// (default 10, at least <see cref="AggregateThreshold"/>).
    /// </summary>
    public int NPlusOneThreshold { get; set; } = 10;

    /// <summary>
    /// A successful root at least this slow is reported at <c>Warning</c> instead of
    /// <c>Information</c> (default 1 second, must be positive).
    /// </summary>
    public TimeSpan SlowThreshold { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Share of fast, successful roots that are reported (default 1.0, range 0.0–1.0). Roots that
    /// failed, were cancelled, were slow, contain a failed step, or look like N+1 are always reported.
    /// </summary>
    public double SampleRate { get; set; } = 1.0;

    /// <summary>
    /// Starts an <see cref="System.Diagnostics.Activity"/> per recorded step on
    /// <c>CoreLoggingActivitySources.OperationsName</c> when that source has listeners (default <c>true</c>).
    /// </summary>
    public bool EmitActivities { get; set; } = true;

    /// <summary>Flight recorder: Debug/Trace entries attached to the report of a failed root (off by default).</summary>
    public ChronicleFlightRecorderOptions FlightRecorder { get; set; } = new();

    /// <summary>
    /// Records every SqlClient command run inside a root as a <c>SQL {keyword}</c> step, observed
    /// through SqlClient's <c>DiagnosticListener</c> (default <c>true</c>). Takes effect when the
    /// host starts its hosted services.
    /// </summary>
    public bool CaptureSQLCommands { get; set; } = true;

    /// <summary>
    /// Records the statement text (attribute <c>Statement</c>) and the error message of a failed
    /// command (default <c>false</c>: SQL text and SqlClient messages can carry literal values).
    /// </summary>
    public bool CaptureSQLCommandText { get; set; }
}
