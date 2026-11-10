using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Options of the Chronicle flight recorder: Debug/Trace entries written inside a root are
/// buffered and attached to the report only when the root fails.
/// </summary>
public sealed class ChronicleFlightRecorderOptions
{
    /// <summary>
    /// Buffers entries when <c>true</c>; default <c>false</c>, because buffering formats every
    /// Debug entry written inside a root.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Lowest buffered level: <see cref="LogLevel.Debug"/> (default) or <see cref="LogLevel.Trace"/>.</summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

    /// <summary>Maximum buffered entries per root (default 100, range 1–1000); later entries are counted only.</summary>
    public int MaxEntries { get; set; } = 100;

    /// <summary>Maximum length of a buffered message (default 500, range 32–4096); longer messages end with <c>[truncated]</c>.</summary>
    public int MaxMessageLength { get; set; } = 500;
}
