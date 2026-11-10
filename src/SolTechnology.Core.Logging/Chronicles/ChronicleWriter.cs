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

    internal string Truncate(string value) => ChronicleText.Truncate(value, Options.MaxAttributeValueLength);

    internal ChronicleStep StartRoot(string name, ChronicleNode? previous, bool withActivity = true)
    {
        name = Truncate(name);
        var activity = withActivity ? ChronicleActivities.Start(Options, name) : null;
        var root = new ChronicleRoot(this, name, TimeProvider.GetTimestamp(), activity);
        AmbientChronicle.Current = root.Node;
        return new ChronicleStep(root.Node, previous, owns: true, activity);
    }

    internal ChronicleStep StartActivityOnly(string name)
        => ChronicleActivities.Start(Options, name) is { } activity ? new ChronicleStep(activity) : default;

    internal ChronicleStep StartChild(ChronicleNode parent, string name, bool withActivity = true)
    {
        var root = parent.Root;
        if (parent.IsClosed)
        {
            root.CountLateStep();
        }

        if (parent.Depth >= Options.MaxDepth)
        {
            parent.CountDroppedDeeper();
            return default;
        }

        name = Truncate(name);
        if (!root.TryReserveStep(Options.MaxSteps))
        {
            return new ChronicleStep(new ChronicleNode(root, parent, name, name, TimeProvider.GetTimestamp(), isOverflow: true), null, owns: true);
        }

        var activity = withActivity ? ChronicleActivities.Start(Options, name) : null;
        var node = new ChronicleNode(root, parent, name, name, TimeProvider.GetTimestamp()) { Activity = activity };
        parent.AddChild(node);
        AmbientChronicle.Current = node;
        return new ChronicleStep(node, parent, owns: true, activity);
    }

    internal void SetAttribute(ChronicleNode node, string key, string value)
    {
        if (!node.IsOverflow)
        {
            node.SetAttribute(key, Truncate(value), Options.MaxAttributesPerStep);
        }
    }

    internal void Fail(ChronicleNode node, string errorType, string? message, Exception? exception)
    {
        node.Fail(errorType, message is null ? null : Truncate(message));
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
