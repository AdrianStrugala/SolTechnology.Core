using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// Source-generated emitter for a handler that returned a failed <see cref="Result"/>. It carries only
/// the <see cref="Error"/> runtime type name and <see cref="Error.Message"/>, never error details or data.
/// </summary>
internal static partial class OperationResultLogMessages
{
    [LoggerMessage(
        EventId = 2149,
        EventName = nameof(OperationFailedWithResult),
        Level = LogLevel.Warning,
        Message = "Operation: [{OperationName}]. Status: [FAIL]. Duration: [{DurationMs} ms]. ErrorType: [{ErrorType}]. ErrorMessage: [{ErrorMessage}]")]
    public static partial void OperationFailedWithResult(
        ILogger logger,
        string operationName,
        long durationMs,
        string errorType,
        string? errorMessage);
}
