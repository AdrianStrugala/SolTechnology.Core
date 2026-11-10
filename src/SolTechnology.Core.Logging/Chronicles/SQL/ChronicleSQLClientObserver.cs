using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SolTechnology.Core.Logging.Chronicles.SQL;

/// <summary>
/// Turns SqlClient command events into leaf steps of the roots its own writer owns. Several hosts
/// in one process each have an observer, and each records a command at most once (CRM-1). Events
/// can arrive while another subscriber enabled them, so every event re-checks its gate (CRM-15).
/// </summary>
internal sealed class ChronicleSQLClientObserver(ChronicleWriter writer) : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    internal const string ListenerName = "SqlClientDiagnosticListener";
    internal const string Before = "Microsoft.Data.SqlClient.WriteCommandBefore";
    internal const string After = "Microsoft.Data.SqlClient.WriteCommandAfter";
    internal const string Error = "Microsoft.Data.SqlClient.WriteCommandError";

    private readonly ConditionalWeakTable<object, PendingSQLStep> _pending = new();
    private readonly List<IDisposable> _subscriptions = [];
    private int _pendingCount;
    private bool _disposed;

    internal int PendingCount => Volatile.Read(ref _pendingCount);

    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name != ListenerName)
        {
            return;
        }

        lock (_subscriptions)
        {
            if (!_disposed)
            {
                _subscriptions.Add(listener.Subscribe(this, IsEnabled));
            }
        }
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        try
        {
            if (value.Key == Before)
            {
                OnBefore(SQLClientPayloadReader.Read(value.Value));
            }
            else if (value.Key is After or Error)
            {
                OnCompleted(SQLClientPayloadReader.Read(value.Value), value.Key == Error);
            }
        }
        catch (Exception)
        {
            // Instrumentation must never fail a SqlClient command.
        }
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void Dispose()
    {
        lock (_subscriptions)
        {
            _disposed = true;
            _subscriptions.ForEach(s => s.Dispose());
            _subscriptions.Clear();
        }
    }

    internal bool IsEnabled(string eventName) => eventName switch
    {
        Before => OwnOpenRoot() is not null,
        After or Error => PendingCount > 0,
        _ => false
    };

    private ChronicleNode? OwnOpenRoot()
        => writer.Options.Enabled && AmbientChronicle.Current is { Root.IsClosed: false } node && node.Root.Owner == writer ? node : null;

    private void OnBefore(SQLClientPayload payload)
    {
        if (OwnOpenRoot() is not { } parent || payload.Command is not { } command || payload.OperationId == Guid.Empty)
        {
            return;
        }

        var text = command.CommandText ?? string.Empty;
        var name = SQLClientPayloadReader.StepName(command);
        if (writer.StartDetachedLeaf(parent, name, $"{name}#{text.GetHashCode()}") is not { } leaf)
        {
            return;
        }

        if (writer.Options.CaptureSQLCommandText)
        {
            writer.SetAttribute(leaf, "Statement", text);
        }

        if (!_pending.TryGetValue(command, out _))
        {
            Interlocked.Increment(ref _pendingCount);
        }

        _pending.AddOrUpdate(command, new PendingSQLStep(payload.OperationId, leaf));
    }

    private void OnCompleted(SQLClientPayload payload, bool failed)
    {
        if (payload.Command is not { } command
            || !_pending.TryGetValue(command, out var pending)
            || pending.OperationId != payload.OperationId
            || !_pending.Remove(command))
        {
            return;
        }

        Interlocked.Decrement(ref _pendingCount);
        if (failed)
        {
            var message = writer.Options.CaptureSQLCommandText ? payload.Exception?.Message : null;
            writer.Fail(pending.Leaf, payload.Exception?.GetType().Name ?? "SQLError", message, null);
        }

        writer.Close(pending.Leaf, restore: null);
    }

    private sealed record PendingSQLStep(Guid OperationId, ChronicleNode Leaf);
}
