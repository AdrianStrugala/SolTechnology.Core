using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>One Debug/Trace entry buffered by the flight recorder inside a failed root.</summary>
public sealed class ChronicleFlightRecord
{
    internal ChronicleFlightRecord()
    {
    }

    /// <summary>Offset from the root start, in milliseconds.</summary>
    public long OffsetMs { get; internal init; }

    /// <summary>Level of the entry.</summary>
    public LogLevel Level { get; internal init; }

    /// <summary>Logger category of the entry.</summary>
    public string Category { get; internal init; } = string.Empty;

    /// <summary>Event id of the entry.</summary>
    public int EventId { get; internal init; }

    /// <summary>Formatted message, cut to <see cref="ChronicleFlightRecorderOptions.MaxMessageLength"/>.</summary>
    public string Message { get; internal init; } = string.Empty;

    /// <summary>Type name of the entry's exception, if any; never the stack trace.</summary>
    public string? ExceptionType { get; internal init; }
}
