namespace SolTechnology.Core.Logging.Masking;

/// <summary>
/// When placed on a property alongside <see cref="LogScopeAttribute"/>, the value is masked
/// via <see cref="PiiMask"/> before being pushed into the logger scope.
/// Has no effect without <see cref="LogScopeAttribute"/> (masking only applies to logged values).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MaskedAttribute : Attribute
{
    /// <summary>Masking strategy.</summary>
    public MaskMode Mode { get; }

    /// <summary>Characters to keep at start and end when <see cref="MaskMode.Partial"/>.</summary>
    public int KeepChars { get; }

    public MaskedAttribute(MaskMode mode = MaskMode.Full, int keepChars = 3)
    {
        Mode = mode;
        KeepChars = keepChars;
    }
}

