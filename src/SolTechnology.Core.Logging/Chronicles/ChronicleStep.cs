using System.Globalization;
using SolTechnology.Core.Logging.Masking;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Handle to one Chronicle step, returned by <see cref="IChronicle"/>. Use it in a
/// <c>using</c> statement: <c>Dispose</c> closes the step. The default value records nothing,
/// so every member is safe to call when Chronicle is disabled or no root is open.
/// </summary>
public readonly struct ChronicleStep : IDisposable
{
    private readonly ChronicleNode? _node;
    private readonly ChronicleNode? _restore;
    private readonly bool _owns;

    internal ChronicleStep(ChronicleNode node, ChronicleNode? restore, bool owns)
    {
        _node = node;
        _restore = restore;
        _owns = owns;
    }

    /// <summary><c>true</c> when this handle records into a Chronicle report.</summary>
    public bool IsRecording => _node is not null;

    /// <summary>
    /// Adds or overwrites an attribute, stored as its invariant-culture string. The value is
    /// logged as is; use <see cref="SetMasked"/> for personal data.
    /// </summary>
    /// <param name="key">Attribute name.</param>
    /// <param name="value">Attribute value.</param>
    /// <returns>This handle, for chaining.</returns>
    public ChronicleStep Set(string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_node is not null)
        {
            _node.Root.Owner.SetAttribute(_node, key, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return this;
    }

    /// <summary>Adds or overwrites an attribute with a masked value; the raw value is never kept.</summary>
    /// <param name="key">Attribute name.</param>
    /// <param name="value">Raw value to mask.</param>
    /// <param name="mode">Masking mode, see <see cref="PiiMask.Apply"/>.</param>
    /// <param name="keepChars">Characters kept at each end for <see cref="MaskMode.Partial"/>.</param>
    /// <returns>This handle, for chaining.</returns>
    public ChronicleStep SetMasked(string key, string? value, MaskMode mode = MaskMode.Full, int keepChars = 3)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_node is not null)
        {
            _node.Root.Owner.SetAttribute(_node, key, PiiMask.Apply(value, mode, keepChars));
        }

        return this;
    }

    /// <summary>Marks the step failed with the exception's type name and message.</summary>
    /// <param name="exception">The failure.</param>
    public void Fail(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _node?.Root.Owner.Fail(_node, exception.GetType().Name, exception.Message, exception);
    }

    /// <summary>Marks the step failed without an exception, for example for a failed <c>Result</c>.</summary>
    /// <param name="errorType">Short error type or code shown in the report.</param>
    /// <param name="message">Optional error message.</param>
    public void Fail(string errorType, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(errorType);
        _node?.Root.Owner.Fail(_node, errorType, message, null);
    }

    /// <summary>Marks the step cancelled.</summary>
    public void Cancel() => _node?.Cancel();

    /// <summary>Closes the step. Idempotent and never throws; a handle from <see cref="IChronicle.Current"/> does nothing.</summary>
    public void Dispose()
    {
        if (_owns && _node is not null)
        {
            _node.Root.Owner.Close(_node, _restore);
        }
    }
}
