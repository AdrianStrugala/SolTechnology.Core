using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace SolTechnology.Core.AUID;

/// <summary>
/// A unique 64-bit identifier (AUID) designed for high-performance scenarios.
/// Structure: [Code: 15 bits] [Timestamp: 32 bits] [Random: 17 bits].
/// Total: 64 bits used.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[TypeConverter(typeof(AuidTypeConverter))]
[JsonConverter(typeof(AuidJsonConverter))]
public readonly partial struct Auid : IComparable<Auid>, IEquatable<Auid>, IParsable<Auid>
{
    // Bits 63-49: code (15 bits), bits 48-17: seconds since the epoch (32 bits), bits 16-0: random (17 bits).
    private const int BitsRandom = 17;
    private const int BitsTime = 32;

    private const long MaskRandom = (1L << BitsRandom) - 1;     // 0x1FFFF
    private const long MaskTime = (1L << BitsTime) - 1;         // 0xFFFFFFFF

    // Epoch: 2001-01-01 UTC
    private static readonly long EpochTicks = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    private const string DefaultCode = "XXX";
    private const char Separator = '_';

    /// <summary>
    /// The raw 64-bit integer value. Can be negative due to full 64-bit usage.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Represents an empty AUID with value 0.
    /// </summary>
    public static readonly Auid Empty = new Auid(0);

    private Auid(long value) => Value = value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Auid other && Equals(other);

    /// <inheritdoc />
    public bool Equals(Auid other) => Value == other.Value;

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(Auid other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Determines whether two AUID instances are equal.
    /// </summary>
    public static bool operator ==(Auid left, Auid right) => left.Value == right.Value;

    /// <summary>
    /// Determines whether two AUID instances are not equal.
    /// </summary>
    public static bool operator !=(Auid left, Auid right) => left.Value != right.Value;

    /// <summary>
    /// Implicitly converts an AUID to its raw 64-bit long value.
    /// </summary>
    public static implicit operator long(Auid a) => a.Value;

    /// <summary>
    /// Explicitly converts a 64-bit long value to an AUID.
    /// </summary>
    public static explicit operator Auid(long v) => new Auid(v);
}
