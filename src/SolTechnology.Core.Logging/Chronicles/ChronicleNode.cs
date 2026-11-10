namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// One recorded step. Children, attributes, status and failure are written under one lock, so
/// concurrent branches that share a node can never throw into the caller.
/// </summary>
internal sealed class ChronicleNode(ChronicleRoot root, ChronicleNode? parent, string name, string groupKey, long startTimestamp)
{
    private readonly Lock _gate = new();
    private List<ChronicleNode>? _children;
    private List<KeyValuePair<string, string>>? _attributes;
    private int _closed;

    internal ChronicleRoot Root { get; } = root;
    internal ChronicleNode? Parent { get; } = parent;
    internal string Name { get; } = name;
    internal string GroupKey { get; } = groupKey;
    internal int Depth { get; } = parent is null ? 0 : parent.Depth + 1;
    internal long StartTimestamp { get; } = startTimestamp;
    internal long EndTimestamp { get; private set; }
    internal ChronicleStepStatus Status { get; private set; }
    internal string? ErrorType { get; private set; }
    internal string? ErrorMessage { get; private set; }
    internal bool IsUnfinished { get; private set; }
    internal bool IsClosed => Volatile.Read(ref _closed) == 1;

    internal void AddChild(ChronicleNode child)
    {
        lock (_gate)
        {
            (_children ??= []).Add(child);
        }
    }

    internal void SetAttribute(string key, string value)
    {
        lock (_gate)
        {
            _attributes ??= [];
            for (var i = 0; i < _attributes.Count; i++)
            {
                if (_attributes[i].Key == key)
                {
                    _attributes[i] = new(key, value);
                    return;
                }
            }

            _attributes.Add(new(key, value));
        }
    }

    internal void Fail(string errorType, string? message)
    {
        lock (_gate)
        {
            Status = ChronicleStepStatus.Failed;
            ErrorType = errorType;
            ErrorMessage = message;
        }
    }

    internal void Cancel()
    {
        lock (_gate)
        {
            Status = ChronicleStepStatus.Cancelled;
        }
    }

    internal bool TryClose(long endTimestamp, bool unfinished = false)
    {
        if (Interlocked.Exchange(ref _closed, 1) == 1)
        {
            return false;
        }

        lock (_gate)
        {
            EndTimestamp = endTimestamp;
            IsUnfinished = unfinished;
        }

        return true;
    }

    internal ChronicleNode[] SnapshotChildren()
    {
        lock (_gate)
        {
            return _children is null ? [] : [.. _children];
        }
    }

    internal KeyValuePair<string, string>[] SnapshotAttributes()
    {
        lock (_gate)
        {
            return _attributes is null ? [] : [.. _attributes];
        }
    }
}
