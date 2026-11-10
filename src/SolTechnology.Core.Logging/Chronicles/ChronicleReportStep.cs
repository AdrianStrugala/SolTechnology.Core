using System.Collections.ObjectModel;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// One line of a <see cref="ChronicleReport"/>: a recorded step, or an aggregated line for
/// siblings that share a name (<see cref="Count"/> greater than one).
/// </summary>
public sealed class ChronicleReportStep
{
    internal ChronicleReportStep()
    {
    }

    /// <summary>Step name.</summary>
    public string Name { get; internal init; } = string.Empty;

    /// <summary>Start offset from the root start, in milliseconds.</summary>
    public long OffsetMs { get; internal init; }

    /// <summary>Duration in milliseconds; for an aggregated line, the total of its steps.</summary>
    public long DurationMs { get; internal init; }

    /// <summary>Outcome; for an aggregated line, <see cref="ChronicleStepStatus.Failed"/> when any of its steps failed.</summary>
    public ChronicleStepStatus Status { get; internal init; }

    /// <summary>Number of steps on this line: 1 for a recorded step.</summary>
    public int Count { get; internal init; } = 1;

    /// <summary>Number of failed steps on this line.</summary>
    public int FailedCount { get; internal init; }

    /// <summary><c>true</c> when the step was still open when its root completed.</summary>
    public bool IsUnfinished { get; internal init; }

    /// <summary><c>true</c> when <see cref="Count"/> reached <see cref="ChronicleOptions.NPlusOneThreshold"/>.</summary>
    public bool IsNPlusOneSuspected { get; internal init; }

    /// <summary>Attributes set on the step; empty for an aggregated line.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; internal init; } = ReadOnlyDictionary<string, string>.Empty;

    /// <summary>Child steps ordered by start offset; empty for an aggregated line.</summary>
    public IReadOnlyList<ChronicleReportStep> Children { get; internal init; } = [];

    internal bool IsAggregated { get; init; }
    internal long MaxDurationMs { get; init; }
    internal string? ErrorType { get; init; }
    internal string? ErrorMessage { get; init; }
    internal int DroppedAttributeCount { get; init; }
    internal int DroppedDeeperCount { get; init; }
    internal int NestedCount { get; init; }
}
