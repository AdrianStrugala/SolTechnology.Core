using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.API.Exceptions;

/// <summary>
/// Status code, <c>type</c> URI and <c>title</c> rules shared by the <see cref="ApiProblemDetailsFactory"/> entry points.
/// </summary>
internal static class ProblemDetailsStatusMap
{
    /// <summary>
    /// Titles of mapped exceptions are fixed texts: exception messages, including BCL ones, can carry
    /// file paths, keys, parameter names, URIs and query strings (CWE-209). 4xx titles are the reason
    /// phrase; client-facing messages travel on the <c>Result</c> path (<see cref="Error.Message"/>).
    /// </summary>
    public static string TitleFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status502BadGateway => "Upstream dependency failed",
        StatusCodes.Status504GatewayTimeout => "Upstream dependency timed out",
        >= StatusCodes.Status500InternalServerError => "An unexpected error occurred",
        _ => ReasonPhrases.GetReasonPhrase(statusCode) is { Length: > 0 } reasonPhrase ? reasonPhrase : "The request was rejected"
    };

    /// <summary>
    /// Maps an <see cref="Error"/> subtype to its HTTP status code. Unknown subtypes default
    /// to <c>500</c> — failure to recognise an error type is a server-side contract drift, not
    /// a client problem.
    /// </summary>
    public static int StatusFor(Error error) => error switch
    {
        ValidationError => StatusCodes.Status400BadRequest,
        UnauthorizedError => StatusCodes.Status401Unauthorized,
        ForbiddenError => StatusCodes.Status403Forbidden,
        NotFoundError => StatusCodes.Status404NotFound,
        ConflictError => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>
    /// Maps a status code to a stable <c>type</c> URI per RFC 7807 §3.1. Pointing to the RFC
    /// section that defines the status keeps the value predictable, dereferenceable, and free
    /// of per-deployment configuration.
    /// </summary>
    public static string TypeForStatus(int status) => status switch
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
