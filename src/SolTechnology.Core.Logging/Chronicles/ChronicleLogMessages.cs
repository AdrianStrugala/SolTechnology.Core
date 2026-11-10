using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>Source-generated Chronicle report entries (<c>EventId</c> 2154).</summary>
internal static partial class ChronicleLogMessages
{
    /// <summary>Logger category of every non-HTTP report; the flight recorder never buffers it.</summary>
    internal const string Category = "SolTechnology.Core.Logging.Chronicle";

    [LoggerMessage(
        EventId = 2154,
        EventName = nameof(ChronicleReported),
        Message = "Chronicle [{ChronicleName}] [{ChronicleStatus}] in [{DurationMs} ms], [{StepCount}] steps: [{@Chronicle}]")]
    internal static partial void ChronicleReported(
        ILogger logger,
        LogLevel level,
        Exception? exception,
        string chronicleName,
        string chronicleStatus,
        long durationMs,
        int stepCount,
        // PascalCase: for a {@Name} placeholder the generator uses the parameter name as the state key.
        ChronicleReport Chronicle);
}
