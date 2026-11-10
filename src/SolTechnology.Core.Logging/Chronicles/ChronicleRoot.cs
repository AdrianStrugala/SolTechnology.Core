namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>One root operation: its root node, wall-clock start, and the counters the report shows.</summary>
internal sealed class ChronicleRoot
{
    private int _closed;
    private int _lateStepCount;

    internal ChronicleRoot(ChronicleRecorder owner, string name, long startTimestamp)
    {
        Owner = owner;
        StartedAt = owner.TimeProvider.GetUtcNow();
        Node = new ChronicleNode(this, null, name, name, startTimestamp);
    }

    internal ChronicleRecorder Owner { get; }
    internal ChronicleNode Node { get; }
    internal DateTimeOffset StartedAt { get; }
    internal Exception? Exception { get; set; }
    internal bool IsClosed => Volatile.Read(ref _closed) == 1;
    internal int LateStepCount => Volatile.Read(ref _lateStepCount);

    internal void CountLateStep() => Interlocked.Increment(ref _lateStepCount);

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
