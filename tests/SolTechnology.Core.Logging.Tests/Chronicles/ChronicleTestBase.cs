using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

/// <summary>Resets the process-wide ambient node around every test and builds recorders.</summary>
public abstract class ChronicleTestBase
{
    protected FakeTimeProvider Time { get; private set; } = null!;
    internal RecordingRootListener Listener { get; private set; } = null!;

    [SetUp]
    public void ResetAmbient()
    {
        AmbientChronicle.Current = null;
        Time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
        Listener = new RecordingRootListener();
    }

    [TearDown]
    public void ClearAmbient() => AmbientChronicle.Current = null;

    internal ChronicleRecorder CreateRecorder(Action<ChronicleOptions>? configure = null)
    {
        var options = new ChronicleOptions();
        configure?.Invoke(options);
        return new ChronicleRecorder(Options.Create(options), Time, [Listener]);
    }

    internal static ChronicleNode NodeOf(ChronicleRoot root, params string[] path)
    {
        var node = root.Node;
        foreach (var name in path)
        {
            node = node.SnapshotChildren().Single(c => c.Name == name);
        }

        return node;
    }
}

internal sealed class RecordingRootListener : IChronicleRootListener
{
    private readonly List<ChronicleRoot> _completed = [];

    public IReadOnlyList<ChronicleRoot> Completed
    {
        get
        {
            lock (_completed)
            {
                return [.. _completed];
            }
        }
    }

    public void OnRootCompleted(ChronicleRoot root)
    {
        lock (_completed)
        {
            _completed.Add(root);
        }
    }
}

internal sealed class ThrowingRootListener : IChronicleRootListener
{
    public void OnRootCompleted(ChronicleRoot root) => throw new InvalidOperationException("listener failed");
}
