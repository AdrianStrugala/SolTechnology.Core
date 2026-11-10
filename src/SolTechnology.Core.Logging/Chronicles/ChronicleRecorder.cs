using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// The <see cref="IChronicle"/> implementation: decides from the ambient node whether a call
/// starts a root, a child, or nothing, and leaves the tree itself to its <see cref="ChronicleWriter"/>.
/// </summary>
internal sealed class ChronicleRecorder(
    IOptions<ChronicleOptions> options,
    TimeProvider timeProvider,
    IEnumerable<IChronicleRootListener> listeners) : IChronicle
{
    internal ChronicleWriter Writer { get; } = new(options.Value, timeProvider, listeners);
    internal ChronicleOptions Options => Writer.Options;

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
            ? Writer.StartRoot(name, ambient)
            : Writer.StartChild(ambient, name);
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
            : Writer.StartChild(ambient, name);
    }
}
