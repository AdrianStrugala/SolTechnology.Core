namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Turns a closed <see cref="ChronicleRoot"/> into a <see cref="ChronicleReport"/>: snapshots
/// every node under its lock, orders siblings by start offset, and collapses siblings that share
/// a group key into aggregated lines, merged with the overflow aggregates of dropped steps.
/// </summary>
internal sealed class ChronicleReportBuilder
{
    private readonly ChronicleRoot _root;
    private readonly ChronicleOptions _options;
    private readonly TimeProvider _time;
    private readonly long _rootEnd;
    private bool _hasFailedSteps;
    private bool _isNPlusOneSuspected;

    private ChronicleReportBuilder(ChronicleRoot root)
    {
        _root = root;
        _options = root.Owner.Options;
        _time = root.Owner.TimeProvider;
        _rootEnd = root.Node.EndTimestamp;
    }

    internal static ChronicleReport Build(ChronicleRoot root) => new ChronicleReportBuilder(root).Build();

    private ChronicleReport Build()
    {
        var rootStep = BuildStep(_root.Node);
        return new ChronicleReport
        {
            Name = _root.Node.Name,
            Status = _root.Node.Status,
            StartedAt = _root.StartedAt,
            DurationMs = rootStep.DurationMs,
            Steps = [rootStep],
            StepCount = _root.StepCount,
            DroppedStepCount = _root.DroppedStepCount,
            LateStepCount = _root.LateStepCount,
            IsNPlusOneSuspected = _isNPlusOneSuspected,
            HasFailedSteps = _hasFailedSteps
        };
    }

    private ChronicleReportStep BuildStep(ChronicleNode node)
    {
        _hasFailedSteps |= node.Status == ChronicleStepStatus.Failed;
        return new ChronicleReportStep
        {
            Name = node.Name,
            OffsetMs = Milliseconds(node.StartTimestamp - _root.Node.StartTimestamp),
            DurationMs = Milliseconds(EndOf(node) - node.StartTimestamp),
            Status = node.Status,
            IsUnfinished = node.IsUnfinished,
            Attributes = node.SnapshotAttributes().ToDictionary(),
            Children = BuildChildren(node),
            ErrorType = node.ErrorType,
            ErrorMessage = node.ErrorMessage,
            DroppedAttributeCount = node.DroppedAttributeCount,
            DroppedDeeperCount = node.DroppedDeeperCount
        };
    }

    private List<ChronicleReportStep> BuildChildren(ChronicleNode node)
    {
        var overflow = node.SnapshotOverflow().ToDictionary();
        var steps = new List<ChronicleReportStep>();
        foreach (var group in node.SnapshotChildren().OrderBy(c => c.StartTimestamp).GroupBy(c => c.GroupKey))
        {
            var hasOverflow = overflow.Remove(group.Key, out var dropped);
            if (hasOverflow || group.Count() >= _options.AggregateThreshold)
            {
                steps.Add(Aggregate([.. group], dropped));
            }
            else
            {
                steps.AddRange(group.Select(BuildStep));
            }
        }

        steps.AddRange(overflow.Values.Select(dropped => Aggregate([], dropped)));
        return [.. steps.OrderBy(s => s.OffsetMs)];
    }

    private ChronicleReportStep Aggregate(ChronicleNode[] nodes, ChronicleOverflow dropped)
    {
        var durations = nodes.Select(n => EndOf(n) - n.StartTimestamp).ToArray();
        var count = nodes.Length + dropped.Count;
        var failed = nodes.Count(n => n.Status == ChronicleStepStatus.Failed) + dropped.FailedCount;
        var firstStart = nodes.Length > 0 ? nodes[0].StartTimestamp : dropped.FirstStartTimestamp;
        if (dropped.Count > 0 && dropped.FirstStartTimestamp < firstStart)
        {
            firstStart = dropped.FirstStartTimestamp;
        }

        var isNPlusOne = count >= _options.NPlusOneThreshold;
        _hasFailedSteps |= failed > 0;
        _isNPlusOneSuspected |= isNPlusOne;
        return new ChronicleReportStep
        {
            Name = nodes.Length > 0 ? nodes[0].Name : dropped.Name,
            OffsetMs = Milliseconds(firstStart - _root.Node.StartTimestamp),
            DurationMs = Milliseconds(durations.Sum() + dropped.TotalTicks),
            MaxDurationMs = Milliseconds(Math.Max(durations.DefaultIfEmpty().Max(), dropped.MaxTicks)),
            Status = failed > 0 ? ChronicleStepStatus.Failed : ChronicleStepStatus.Ok,
            Count = count,
            FailedCount = failed,
            IsUnfinished = nodes.Any(n => n.IsUnfinished),
            IsNPlusOneSuspected = isNPlusOne,
            IsAggregated = true,
            NestedCount = nodes.Sum(CountDescendants)
        };
    }

    private int CountDescendants(ChronicleNode node)
    {
        var count = 0;
        foreach (var child in node.SnapshotChildren())
        {
            _hasFailedSteps |= child.Status == ChronicleStepStatus.Failed;
            count += 1 + CountDescendants(child);
        }

        foreach (var (_, dropped) in node.SnapshotOverflow())
        {
            _hasFailedSteps |= dropped.FailedCount > 0;
            count += dropped.Count;
        }

        return count;
    }

    private long EndOf(ChronicleNode node) => node.IsClosed ? node.EndTimestamp : _rootEnd;

    private long Milliseconds(long ticks) => (long)_time.GetElapsedTime(0, ticks).TotalMilliseconds;
}
