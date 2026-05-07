using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.API.Exceptions;

/// <summary>
/// Builds <see cref="Error"/> instances suitable for HTTP response bodies.
/// <para>
/// <strong>Never</strong> includes the raw stack trace in the produced <see cref="Error.Description"/>
/// unless <see cref="ApiExceptionOptions.IncludeExceptionDetails"/> is explicitly enabled.
/// Leaking exception stack traces over HTTP is CWE-209 (Information Exposure Through an Error Message).
/// </para>
/// <para>
/// This factory replaces <c>SolTechnology.Core.CQRS.Errors.Error.From(Exception)</c> on the API
/// boundary, because the latter unconditionally puts <c>exception.StackTrace</c> into
/// <c>Description</c>. <c>Error.From</c> is fine for internal CQRS pipeline use; it is not safe
/// for serialization over the wire.
/// </para>
/// </summary>
internal static class ApiErrorFactory
{
    /// <summary>
    /// Builds an <see cref="Error"/> for an HTTP response body.
    /// </summary>
    /// <param name="exception">The original exception (used only for diagnostics when
    /// <see cref="ApiExceptionOptions.IncludeExceptionDetails"/> is enabled).</param>
    /// <param name="userMessage">Public, user-facing message. Must NOT contain raw exception
    /// internals (SQL schemas, file paths, secrets). Examples: <c>"Validation failed"</c>,
    /// <c>"Resource not found"</c>, <c>"An unexpected error occurred"</c>.</param>
    /// <param name="userDescription">Optional user-facing description. <c>null</c> when no extra
    /// context is appropriate (e.g. unmapped 5xx in Production).</param>
    /// <param name="correlationId">Correlation id to echo on the response body so the client can
    /// quote it in support tickets. Source of truth: <c>ICorrelationIdService</c>. Null is
    /// allowed (e.g. when <c>UseCoreLogging</c> is not registered) — the field is then omitted
    /// only if the consumer's JsonOptions ignore nulls.</param>
    /// <param name="options">Behavior switches. Caller is responsible for resolving these from DI.</param>
    public static Error Build(
        Exception exception,
        string userMessage,
        string? userDescription,
        string? correlationId,
        ApiExceptionOptions options)
    {
        var description = userDescription;

        if (options.IncludeExceptionDetails)
        {
            // Diagnostic block — only ever emitted when the consumer opted in (typically Development).
            // Format is human-readable; not a stable contract for clients.
            var diagnostic =
                $"[Diagnostic] {exception.GetType().FullName}: {exception.Message}" +
                Environment.NewLine +
                exception.StackTrace;

            description = string.IsNullOrEmpty(description)
                ? diagnostic
                : description + Environment.NewLine + Environment.NewLine + diagnostic;
        }

        return new Error
        {
            Message = userMessage,
            Description = description,
            CorrelationId = correlationId
        };
    }
}

