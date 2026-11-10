namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>The bounded flight-recorder buffer of one root. A slot is reserved before the message is formatted.</summary>
internal sealed class ChronicleFlightBuffer
{
    private readonly Lock _gate = new();
    private readonly List<ChronicleFlightRecord> _records = [];
    private int _reserved;
    private int _droppedCount;

    internal int DroppedCount => Volatile.Read(ref _droppedCount);

    internal bool TryReserve(int maxEntries)
    {
        if (Interlocked.Increment(ref _reserved) <= maxEntries)
        {
            return true;
        }

        Interlocked.Decrement(ref _reserved);
        Interlocked.Increment(ref _droppedCount);
        return false;
    }

    internal void Add(ChronicleFlightRecord record)
    {
        lock (_gate)
        {
            _records.Add(record);
        }
    }

    internal ChronicleFlightRecord[] Snapshot()
    {
        lock (_gate)
        {
            return [.. _records];
        }
    }
}
