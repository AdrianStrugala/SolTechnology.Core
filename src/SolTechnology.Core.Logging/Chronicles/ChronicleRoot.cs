namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>One root operation: its root node, wall-clock start, and the counters the report shows.</summary>
internal sealed class ChronicleRoot
{
    private int _closed;
    private int _lateStepCount;
    private int _stepCount = 1;
    private int _droppedStepCount;

    internal ChronicleRoot(ChronicleWriter owner, string name, long startTimestamp)
    {
        Owner = owner;
        StartedAt = owner.TimeProvider.GetUtcNow();
        Node = new ChronicleNode(this, null, name, name, startTimestamp);
    }

    internal ChronicleWriter Owner { get; }
    internal ChronicleNode Node { get; }
    internal DateTimeOffset StartedAt { get; }
    internal Exception? Exception { get; set; }
    internal bool IsClosed => Volatile.Read(ref _closed) == 1;
    internal int LateStepCount => Volatile.Read(ref _lateStepCount);
    internal int StepCount => Volatile.Read(ref _stepCount);
    internal int DroppedStepCount => Volatile.Read(ref _droppedStepCount);

    internal void CountLateStep() => Interlocked.Increment(ref _lateStepCount);

    /// <summary>Reserves one recorded node under <paramref name="maxSteps"/>; otherwise counts a dropped step.</summary>
    internal bool TryReserveStep(int maxSteps)
    {
        if (Interlocked.Increment(ref _stepCount) <= maxSteps)
        {
            return true;
        }

        Interlocked.Decrement(ref _stepCount);
        Interlocked.Increment(ref _droppedStepCount);
        return false;
    }

    /// <summary>Closes the root once and marks every still-open descendant unfinished at <paramref name="endTimestamp"/>.</summary>
    internal bool TryComplete(long endTimestamp)
    {
        if (Interlocked.Exchange(ref _closed, 1) == 1)
        {
            return false;
        }

        MarkUnfinished(Node, endTimestamp);
        return true;
    }

    private static void MarkUnfinished(ChronicleNode node, long endTimestamp)
    {
        foreach (var child in node.SnapshotChildren())
        {
            child.TryClose(endTimestamp, unfinished: true);
            MarkUnfinished(child, endTimestamp);
        }
    }
}
