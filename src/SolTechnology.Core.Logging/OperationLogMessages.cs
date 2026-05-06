using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging;

/// <summary>
/// Source-generated, allocation-free emitters for the operation-lifecycle log events
/// (<c>EventId</c> 2137 / 2138 / 2139 / 2140). These are the "hot path" for the CQRS
/// pipeline behavior, which auto-tracks every MediatR request — at 1k RPS the savings
/// over <c>ILogger.Log(...)</c> with <c>params object[]</c> are real.
/// </summary>
/// <remarks>
/// Internal: consumers call the public extensions on <see cref="ILoggerExtensions"/>,
/// which forward here.
/// </remarks>
internal static partial class OperationLogMessages
{
    [LoggerMessage(
        EventId = 2137,
        EventName = nameof(OperationStarted),
        Level = LogLevel.Information,
        Message = "Operation: [{OperationName}]. Status: [START]")]
    public static partial void OperationStarted(ILogger logger, string operationName);

    [LoggerMessage(
        EventId = 2138,
        EventName = nameof(OperationSucceeded),
        Level = LogLevel.Information,
        Message = "Operation: [{OperationName}]. Status: [SUCCESS]. Duration: [{DurationMs} ms]")]
    public static partial void OperationSucceeded(ILogger logger, string operationName, long durationMs);

    [LoggerMessage(
        EventId = 2139,
        EventName = nameof(OperationFailed),
        Level = LogLevel.Error,
        Message = "Operation: [{OperationName}]. Status: [FAIL]. Duration: [{DurationMs} ms]. Message: [{Message}]")]
    public static partial void OperationFailed(
        ILogger logger,
        Exception? exception,
        string operationName,
        long durationMs,
        string? message);

    /// <summary>
    /// Emits an arbitrary user-supplied string as a <c>{Message}</c> structured property.
    /// Wrapping is required because callers' messages may contain raw <c>{</c> / <c>}</c>
    /// that would otherwise blow up the structured-logging formatter
    /// (<see cref="FormatException"/>).
    /// </summary>
    [LoggerMessage(
        EventId = 2140,
        EventName = nameof(UserMessage),
        Level = LogLevel.Information,
        Message = "[{Message}]")]
    public static partial void UserMessage(ILogger logger, string message);
}

