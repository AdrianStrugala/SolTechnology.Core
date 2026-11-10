using System.Runtime.InteropServices;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// The overflow aggregates of one node, keyed by group key. Capped at <see cref="MaxGroups"/>
/// keys; later keys fold into <see cref="OtherKey"/>, so a long-lived root cannot grow it forever.
/// Not thread-safe: the owning node calls it under its lock.
/// </summary>
internal sealed class ChronicleOverflowGroups
{
    internal const int MaxGroups = 64;
    internal const string OtherKey = "(other)";

    private readonly Dictionary<string, ChronicleOverflow> _groups = [];

    internal void Add(string groupKey, long startTimestamp, long elapsedTicks, bool failed)
    {
        var key = _groups.Count < MaxGroups || _groups.ContainsKey(groupKey) ? groupKey : OtherKey;
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_groups, key, out _);
        entry.Add(startTimestamp, elapsedTicks, failed);
    }

    internal KeyValuePair<string, ChronicleOverflow>[] Snapshot() => [.. _groups];
}
