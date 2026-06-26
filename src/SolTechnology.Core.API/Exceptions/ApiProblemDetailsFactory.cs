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
            Title = exception.Message,
            Type = TypeForStatus(statusCode)
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
        var statusCode = StatusFor(error);

        ProblemDetails problem;
        if (error is ValidationError validationError)
        {
            problem = new ValidationProblemDetails(validationError.Errors.ToDictionary(
                kv => kv.Key,
                kv => kv.Value,
                StringComparer.Ordinal))
            {
                Status = statusCode,
                Title = string.IsNullOrEmpty(error.Message)
                    ? "One or more validation errors occurred."
                    : error.Message,
                Detail = error.Description,
                Type = TypeForStatus(statusCode)
            };
        }
        else
        {
            problem = new ProblemDetails
            {
                Status = statusCode,
                Title = error.Message,
                Detail = error.Description,
                Type = TypeForStatus(statusCode)
            };
        }

        var corr = correlationId ?? error.CorrelationId;
        if (!string.IsNullOrEmpty(corr))
        {
            problem.Extensions[CorrelationIdKey] = corr;
        }

        // Always emit — absence would be ambiguous for the client.
        problem.Extensions[RecoverableKey] = error.Recoverable;

        return problem;
    }

    /// <summary>
    /// Maps an <see cref="Error"/> subtype to its HTTP status code. Unknown subtypes default
    /// to <c>500</c> — failure to recognise an error type is a server-side contract drift, not
    /// a client problem.
    /// </summary>
    private static int StatusFor(Error error) => error switch
    {
        ValidationError => StatusCodes.Status400BadRequest,
        UnauthorizedError => StatusCodes.Status401Unauthorized,
        ForbiddenError => StatusCodes.Status403Forbidden,
        NotFoundError => StatusCodes.Status404NotFound,
        ConflictError => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

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
            Type = TypeForStatus(StatusCodes.Status400BadRequest)
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

    /// <summary>
    /// Maps a status code to a stable <c>type</c> URI per RFC 7807 §3.1. Pointing to the RFC
    /// section that defines the status keeps the value predictable, dereferenceable, and free
    /// of per-deployment configuration.
    /// </summary>
    private static string TypeForStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        StatusCodes.Status422UnprocessableEntity => "https://tools.ietf.org/html/rfc4918#section-11.2",
        StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        StatusCodes.Status501NotImplemented => "https://tools.ietf.org/html/rfc9110#section-15.6.2",
        StatusCodes.Status503ServiceUnavailable => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
        StatusCodes.Status504GatewayTimeout => "https://tools.ietf.org/html/rfc9110#section-15.6.5",
        _ => $"https://httpstatuses.io/{status}"
    };
}


