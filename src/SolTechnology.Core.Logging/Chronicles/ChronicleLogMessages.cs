using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>Source-generated Chronicle report entries: <c>EventId</c> 2154 per root, 2155 per HTTP request root.</summary>
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

    [LoggerMessage(
        EventId = 2155,
        EventName = nameof(RequestChronicleReported),
        Message = "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]. Chronicle: [{ChronicleStatus}] [{@Chronicle}]")]
    internal static partial void RequestChronicleReported(
        ILogger logger,
        LogLevel level,
        Exception? exception,
        string requestMethod,
        PathString requestPath,
        int statusCode,
        long elapsedMs,
        string chronicleStatus,
        // PascalCase: for a {@Name} placeholder the generator uses the parameter name as the state key.
        ChronicleReport Chronicle);
}
