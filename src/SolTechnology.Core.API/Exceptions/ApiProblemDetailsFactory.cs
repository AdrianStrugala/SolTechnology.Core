using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.API.Exceptions;

/// <summary>
/// Builds RFC 7807 / RFC 9457 <see cref="ProblemDetails"/> instances for the HTTP response body.
/// <para>
/// Two entry points cover both source paths:
/// </para>
/// <list type="bullet">
///   <item><see cref="FromException"/> — used by the exception filter when a mapped exception
///         is thrown out of the pipeline.</item>
///   <item><see cref="FromError"/> — used by the result conversion filter when the application
///         layer returns a failed <c>Result</c> / <c>Result&lt;T&gt;</c>. Status code is
///         derived from the <see cref="Error"/> subtype; the resulting <see cref="ProblemDetails.Status"/>
///         is set on the returned instance.</item>
/// </list>
/// <para>
/// Sets <c>Extensions["correlationId"]</c> so the value resolves to the same logs in
/// Seq / Application Insights as the <c>X-Correlation-Id</c> response header. Diagnostic
/// detail (exception type + stack trace) is added under <c>Extensions["exception"]</c>
/// **only** when <see cref="ApiExceptionOptions.IncludeExceptionDetails"/> is enabled.
/// </para>
/// </summary>
internal static class ApiProblemDetailsFactory
{
    public const string CorrelationIdKey = "correlationId";
    public const string RecoverableKey = "recoverable";
    public const string ExceptionKey = "exception";

    /// <summary>
    /// Builds <see cref="ProblemDetails"/> (or <see cref="ValidationProblemDetails"/> for
    /// <see cref="ValidationException"/>) from a thrown exception that the exception filter
    /// mapped to a known status code.
    /// </summary>
    public static ProblemDetails FromException(
        Exception exception,
        int statusCode,
        string? correlationId,
        ApiExceptionOptions options)
    {
        if (exception is ValidationException validationException)
        {
            return BuildValidationFromException(validationException, correlationId, options);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ProblemDetailsStatusMap.TitleFor(exception, statusCode),
            Type = ProblemDetailsStatusMap.TypeForStatus(statusCode)
        };
        ApplyExtensions(problem, exception, correlationId, options);

        // Conservative default: unmapped 5xx is a transient server fault (worth retrying);
        // mapped 4xx is a deterministic client/business rejection (retry is pointless).
        problem.Extensions[RecoverableKey] = statusCode >= StatusCodes.Status500InternalServerError;

        return problem;
    }

    /// <summary>
    /// Builds <see cref="ProblemDetails"/> from an <see cref="Error"/> returned by the
    /// application layer in a failed <c>Result</c>. The HTTP status code is derived from the
    /// concrete <see cref="Error"/> subtype and written to <see cref="ProblemDetails.Status"/>.
    /// No exception is involved — the diagnostic extension is therefore never added (no stack
    /// trace to leak).
    /// </summary>
    public static ProblemDetails FromError(Error error, string? correlationId)
    {
        var statusCode = ProblemDetailsStatusMap.StatusFor(error);

        ProblemDetails problem = error is ValidationError validationError
            ? new ValidationProblemDetails(validationError.Errors.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal))
            {
                Title = string.IsNullOrEmpty(error.Message) ? "One or more validation errors occurred." : error.Message
            }
            : new ProblemDetails { Title = error.Message };
        problem.Status = statusCode;
        problem.Detail = error.Description;
        problem.Type = ProblemDetailsStatusMap.TypeForStatus(statusCode);

        var corr = correlationId ?? error.CorrelationId;
        if (!string.IsNullOrEmpty(corr))
        {
            problem.Extensions[CorrelationIdKey] = corr;
        }

        // Always emit — absence would be ambiguous for the client.
        problem.Extensions[RecoverableKey] = error.Recoverable;

        return problem;
    }

    private static ValidationProblemDetails BuildValidationFromException(
        ValidationException exception,
        string? correlationId,
        ApiExceptionOptions options)
    {
        // FluentValidation.ValidationException.Errors is an IEnumerable<ValidationFailure>.
        // Group by PropertyName so the same field collects all rule failures.
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = ProblemDetailsStatusMap.TypeForStatus(StatusCodes.Status400BadRequest)
        };
        ApplyExtensions(problem, exception, correlationId, options);
        problem.Extensions[RecoverableKey] = false;
        return problem;
    }

    private static void ApplyExtensions(
        ProblemDetails problem,
        Exception exception,
        string? correlationId,
        ApiExceptionOptions options)
    {
        if (!string.IsNullOrEmpty(correlationId))
        {
            problem.Extensions[CorrelationIdKey] = correlationId;
        }

        if (options.IncludeExceptionDetails)
        {
            // Diagnostic block — only ever emitted when the consumer opted in (typically Development).
            // Format is human-readable; not a stable contract for clients.
            problem.Extensions[ExceptionKey] = new Dictionary<string, string?>
            {
                ["type"] = exception.GetType().FullName,
                ["message"] = exception.Message,
                ["stackTrace"] = exception.StackTrace
            };
        }
    }
}
