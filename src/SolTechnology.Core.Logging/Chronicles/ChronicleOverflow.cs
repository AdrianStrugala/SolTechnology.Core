namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>Aggregate of the steps of one group key that were not recorded because of <see cref="ChronicleOptions.MaxSteps"/>.</summary>
internal struct ChronicleOverflow
{
    internal int Count { get; private set; }
    internal long TotalTicks { get; private set; }
    internal long MaxTicks { get; private set; }
    internal int FailedCount { get; private set; }
    internal long FirstStartTimestamp { get; private set; }

    internal void Add(long startTimestamp, long elapsedTicks, bool failed)
    {
        if (Count == 0 || startTimestamp < FirstStartTimestamp)
        {
            FirstStartTimestamp = startTimestamp;
        }

        Count++;
        TotalTicks += elapsedTicks;
        MaxTicks = Math.Max(MaxTicks, elapsedTicks);
        FailedCount += failed ? 1 : 0;
    }
}
