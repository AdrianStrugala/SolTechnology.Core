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
            ? new ChronicleStep(node, null, owns: false, node.Activity)
            : default;

    public ChronicleStep Begin(string name) => Begin(name, withActivity: true);

    public ChronicleStep Step(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var ambient = AmbientChronicle.Current;
        return !Options.Enabled || ambient is null || ambient.Root.IsClosed
            ? Writer.StartActivityOnly(name)
            : Writer.StartChild(ambient, name);
    }

    /// <summary>A <see cref="Begin(string)"/> for hosts that already own the operation's activity (HTTP request roots).</summary>
    internal ChronicleStep BeginWithoutActivity(string name) => Begin(name, withActivity: false);

    private ChronicleStep Begin(string name, bool withActivity)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!Options.Enabled)
        {
            return withActivity ? Writer.StartActivityOnly(name) : default;
        }

        var ambient = AmbientChronicle.Current;
        return ambient is null || ambient.Root.IsClosed
            ? Writer.StartRoot(name, ambient, withActivity)
            : Writer.StartChild(ambient, name, withActivity);
    }
}
