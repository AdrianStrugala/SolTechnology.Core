namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Writes the bounded step tree of the roots one <see cref="ChronicleRecorder"/> owns: applies
/// the <see cref="ChronicleOptions"/> limits, moves the ambient node, and hands closed roots to
/// the listeners. Every member is safe to call concurrently and never throws into the caller.
/// </summary>
internal sealed class ChronicleWriter(ChronicleOptions options, TimeProvider timeProvider, IEnumerable<IChronicleRootListener> listeners)
{
    private readonly IChronicleRootListener[] _listeners = [.. listeners];

    internal ChronicleOptions Options { get; } = options;
    internal TimeProvider TimeProvider { get; } = timeProvider;

    /// <summary>Level and sampling of the reports a caller emits itself (HTTP request roots).</summary>
    internal ChronicleEmissionPolicy Policy { get; } = new(options);

    internal string Truncate(string value) => ChronicleText.Truncate(value, Options.MaxAttributeValueLength);

    internal ChronicleStep StartRoot(
        string name, ChronicleNode? previous, bool withActivity = true, string? detachedFrom = null, bool reportedByCaller = false)
    {
        name = Truncate(name);
        var activity = withActivity ? ChronicleActivities.Start(Options, name) : null;
        var root = new ChronicleRoot(this, name, TimeProvider.GetTimestamp(), activity)
        {
            DetachedFrom = detachedFrom,
            IsReportedByCaller = reportedByCaller
        };
        AmbientChronicle.Current = root.Node;
        return new ChronicleStep(root.Node, previous, owns: true, activity);
    }

    internal ChronicleStep StartActivityOnly(string name)
        => ChronicleActivities.Start(Options, name) is { } activity ? new ChronicleStep(activity) : default;

    internal ChronicleStep StartChild(ChronicleNode parent, string name, bool withActivity = true)
    {
        var node = AddChild(parent, name, groupKey: null);
        if (node is null || node.IsOverflow)
        {
            return node is null ? default : new ChronicleStep(node, null, owns: true);
        }

        node.Activity = withActivity ? ChronicleActivities.Start(Options, node.Name) : null;
        AmbientChronicle.Current = node;
        return new ChronicleStep(node, parent, owns: true, node.Activity);
    }

    /// <summary>A child that never becomes ambient and starts no activity (SQL commands); <c>null</c> beyond <see cref="ChronicleOptions.MaxDepth"/>.</summary>
    internal ChronicleNode? StartDetachedLeaf(ChronicleNode parent, string name, string groupKey) => AddChild(parent, name, groupKey);

    private ChronicleNode? AddChild(ChronicleNode parent, string name, string? groupKey)
    {
        var root = parent.Root;
        if (parent.IsClosed)
        {
            root.CountLateStep();
        }

        if (parent.Depth >= Options.MaxDepth)
        {
            parent.CountDroppedDeeper();
            return null;
        }

        name = Truncate(name);
        if (!root.TryReserveStep(Options.MaxSteps))
        {
            return new ChronicleNode(root, parent, name, groupKey ?? name, TimeProvider.GetTimestamp(), isOverflow: true);
        }

        var node = new ChronicleNode(root, parent, name, groupKey ?? name, TimeProvider.GetTimestamp());
        parent.AddChild(node);
        return node;
    }

    internal void SetAttribute(ChronicleNode node, string key, string value)
    {
        if (!node.IsOverflow)
        {
            node.SetAttribute(Truncate(key), Truncate(value), Options.MaxAttributesPerStep);
        }
    }

    internal void Fail(ChronicleNode node, string errorType, string? message, Exception? exception)
    {
        node.Fail(Truncate(errorType), message is null ? null : Truncate(message));
        if (exception is not null && node.Parent is null)
        {
            node.Root.Exception = exception;
        }
    }

    internal void Close(ChronicleNode node, ChronicleNode? restore)
    {
        var end = TimeProvider.GetTimestamp();
        var closed = node.TryClose(end);
        if (node.IsOverflow)
        {
            if (closed)
            {
                node.Parent!.AddOverflow(node.Name, node.GroupKey, node.StartTimestamp, end - node.StartTimestamp, node.Status == ChronicleStepStatus.Failed);
            }

            return;
        }

        if (AmbientChronicle.Current == node)
        {
            AmbientChronicle.Current = restore;
        }

        if (closed && node.Parent is null && node.Root.TryComplete(end))
        {
            Notify(node.Root);
        }
    }

    private void Notify(ChronicleRoot root)
    {
        foreach (var listener in _listeners)
        {
            try
            {
                listener.OnRootCompleted(root);
            }
            catch (Exception)
            {
                // Instrumentation must never fail the caller's using block; the reporter logs its own failures.
            }
        }
    }
}
