using Microsoft.AspNetCore.Http;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.API.Exceptions;

/// <summary>
/// Status code, <c>type</c> URI and <c>title</c> rules shared by the <see cref="ApiProblemDetailsFactory"/> entry points.
/// </summary>
internal static class ProblemDetailsStatusMap
{
    /// <summary>
    /// 4xx titles keep the exception message (a client-facing rejection); 5xx titles are fixed,
    /// because server and upstream messages can carry URIs, query strings and keys (CWE-209).
    /// </summary>
    public static string TitleFor(Exception exception, int statusCode) => statusCode switch
    {
        < StatusCodes.Status500InternalServerError => exception.Message,
        StatusCodes.Status502BadGateway => "Upstream dependency failed",
        StatusCodes.Status504GatewayTimeout => "Upstream dependency timed out",
        _ => "An unexpected error occurred"
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
