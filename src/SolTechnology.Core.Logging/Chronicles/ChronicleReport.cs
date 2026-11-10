namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Read-only report of one completed Chronicle root: the structured payload of the
/// <c>{@Chronicle}</c> log property. <see cref="ToString"/> renders the ASCII step tree.
/// </summary>
public sealed class ChronicleReport
{
    private string? _rendered;

    internal ChronicleReport()
    {
    }

    /// <summary>Name of the root, for example the request path or the job name.</summary>
    public string Name { get; internal init; } = string.Empty;

    /// <summary>Outcome of the root.</summary>
    public ChronicleStepStatus Status { get; internal init; }

    /// <summary>Wall-clock start of the root.</summary>
    public DateTimeOffset StartedAt { get; internal init; }

    /// <summary>Duration of the root in milliseconds.</summary>
    public long DurationMs { get; internal init; }

    /// <summary>W3C trace id of the <see cref="System.Diagnostics.Activity"/> current at root start, if any.</summary>
    public string? TraceId { get; internal init; }

    /// <summary>The step tree: one entry, the root step, whose <see cref="ChronicleReportStep.Children"/> hold the recorded steps.</summary>
    public IReadOnlyList<ChronicleReportStep> Steps { get; internal init; } = [];

    /// <summary>Recorded steps, the root included.</summary>
    public int StepCount { get; internal init; }

    /// <summary>Steps beyond <see cref="ChronicleOptions.MaxSteps"/>; they appear only in aggregated lines.</summary>
    public int DroppedStepCount { get; internal init; }

    /// <summary>Steps started under an already closed parent step while the root was open.</summary>
    public int LateStepCount { get; internal init; }

    /// <summary><c>true</c> when an aggregated line reached <see cref="ChronicleOptions.NPlusOneThreshold"/>.</summary>
    public bool IsNPlusOneSuspected { get; internal init; }

    /// <summary>Debug/Trace entries buffered by the flight recorder; filled only for a <see cref="ChronicleStepStatus.Failed"/> root.</summary>
    public IReadOnlyList<ChronicleFlightRecord> FlightRecords { get; internal init; } = [];

    /// <summary>Flight-recorder entries beyond <see cref="ChronicleFlightRecorderOptions.MaxEntries"/>; set only for a failed root.</summary>
    internal int DroppedFlightRecordCount { get; init; }

    /// <summary><c>true</c> when any step, aggregated line, or dropped step failed, whatever the root status.</summary>
    internal bool HasFailedSteps { get; init; }

    /// <summary>The ASCII step tree, starting with a new line. Rendered once and cached.</summary>
    public override string ToString() => _rendered ??= ChronicleTreeRenderer.Render(this);
}
