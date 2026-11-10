using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// The <see cref="IChronicle"/> implementation: keeps the ambient node, builds the tree, and
/// hands each closed root to the <see cref="IChronicleRootListener"/>s.
/// </summary>
internal sealed class ChronicleRecorder(
    IOptions<ChronicleOptions> options,
    TimeProvider timeProvider,
    IEnumerable<IChronicleRootListener> listeners) : IChronicle
{
    private readonly IChronicleRootListener[] _listeners = [.. listeners];

    internal ChronicleOptions Options { get; } = options.Value;
    internal TimeProvider TimeProvider { get; } = timeProvider;

    public bool IsActive => Options.Enabled && AmbientChronicle.OpenRoot is not null;

    public ChronicleStep Current
        => Options.Enabled && AmbientChronicle.Current is { Root.IsClosed: false } node
            ? new ChronicleStep(node, null, owns: false)
            : default;

    public ChronicleStep Begin(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!Options.Enabled)
        {
            return default;
        }

        var ambient = AmbientChronicle.Current;
        return ambient is null || ambient.Root.IsClosed
            ? StartRoot(name, ambient)
            : StartChild(ambient, name);
    }

    public ChronicleStep Step(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!Options.Enabled)
        {
            return default;
        }

        var ambient = AmbientChronicle.Current;
        return ambient is null || ambient.Root.IsClosed
            ? default
            : StartChild(ambient, name);
    }

    internal void SetAttribute(ChronicleNode node, string key, string value) => node.SetAttribute(key, value);

    internal void Fail(ChronicleNode node, string errorType, string? message, Exception? exception)
    {
        node.Fail(errorType, message);
        if (exception is not null && node.Parent is null)
        {
            node.Root.Exception = exception;
        }
    }

    internal void Close(ChronicleNode node, ChronicleNode? restore)
    {
        var end = TimeProvider.GetTimestamp();
        var closed = node.TryClose(end);
        if (AmbientChronicle.Current == node)
        {
            AmbientChronicle.Current = restore;
        }

        if (closed && node.Parent is null && node.Root.TryComplete(end))
        {
            Notify(node.Root);
        }
    }

    private ChronicleStep StartRoot(string name, ChronicleNode? previous)
    {
        var root = new ChronicleRoot(this, name, TimeProvider.GetTimestamp());
        AmbientChronicle.Current = root.Node;
        return new ChronicleStep(root.Node, previous, owns: true);
    }

    private ChronicleStep StartChild(ChronicleNode parent, string name)
    {
        if (parent.IsClosed)
        {
            parent.Root.CountLateStep();
        }

        var node = new ChronicleNode(parent.Root, parent, name, name, TimeProvider.GetTimestamp());
        parent.AddChild(node);
        AmbientChronicle.Current = node;
        return new ChronicleStep(node, parent, owns: true);
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
