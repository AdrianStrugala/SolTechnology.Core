using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.API.Filters;

/// <summary>
/// Converts <c>Result</c> / <c>Result&lt;T&gt;</c> values returned from controller actions into
/// the wire-format expected by HTTP clients:
/// <list type="bullet">
///   <item>Successful <c>Result&lt;T&gt;</c> → HTTP 200 with <c>Data</c> as the raw body.</item>
///   <item>Successful non-generic <c>Result</c> → HTTP 204 No Content.</item>
///   <item>Successful result in a non-200 2xx result (<c>CreatedAtAction</c>, <c>Accepted</c>) → the
///         same result type, status code and <c>Location</c>, with <c>Data</c> as the body.</item>
///   <item>Failed result (either flavor) → RFC 7807 <see cref="ProblemDetails"/>. The HTTP status
///         is derived from the <see cref="Error"/> subtype:
///         <see cref="NotFoundError"/> → 404, <see cref="ConflictError"/> → 409,
///         <see cref="ValidationError"/> → 400 (<see cref="ValidationProblemDetails"/>),
///         <see cref="UnauthorizedError"/> → 401, <see cref="ForbiddenError"/> → 403,
///         everything else → 500.</item>
///   <item>Action returning a bare <see cref="Error"/> (e.g. <c>BadRequest(error)</c>) →
///         <see cref="ProblemDetails"/> at the type-derived status. The MVC helper's status
///         hint is intentionally ignored — the <see cref="Error"/> subtype is the source of truth.</item>
/// </list>
/// <para>
/// Runs as <see cref="IAsyncResultFilter"/> so it observes the <see cref="ObjectResult"/> after
/// MVC builds it but before the body is serialized — replacing <c>context.Result</c> in place is
/// safe and content negotiation still applies.
/// </para>
/// <para>
/// The application layer (CQRS handlers) keeps producing <c>Result&lt;T&gt;</c>; handlers do not
/// know about HTTP. The HTTP boundary is this filter.
/// </para>
/// </summary>
public sealed class ResultConversionFilter(ICorrelationIdService correlationIdService) : IAsyncResultFilter
{
    /// <summary>
    /// Replaces a <c>Result</c>, <c>Result&lt;T&gt;</c> or bare <see cref="Error"/> in the action's
    /// <see cref="ObjectResult"/> with its wire format, then runs the rest of the pipeline.
    /// </summary>
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: { } value } objectResult)
        {
            // Static dispatch, no reflection (AOT/trim safe); a bare Error covers the BadRequest(error) shorthand.
            if (value is Result result)
            {
                context.Result = Convert(result, objectResult);
            }
            else if (value is Error error)
            {
                context.Result = BuildProblem(error);
            }
        }

        return next();
    }

    private IActionResult Convert(Result result, ObjectResult objectResult)
    {
        if (!result.IsSuccess)
        {
            return BuildProblem(result.Error ?? new Error { Message = "Operation failed." });
        }

        // Success: GetData() returns the payload for Result<T> or null for the non-generic
        // base. See Result.GetData() remarks.
        var data = result.GetData();

        // ActionResult<Result<T>> declares Result<T>; the payload must be serialized as itself.
        objectResult.DeclaredType = null;

        // CreatedAtAction / Accepted: keep the result type, its status and its Location.
        if (IsNon200Success(objectResult.StatusCode))
        {
            objectResult.Value = data;
            return objectResult;
        }

        if (data is null)
        {
            return new StatusCodeResult(StatusCodes.Status204NoContent);
        }

        objectResult.Value = data;
        objectResult.StatusCode = StatusCodes.Status200OK;
        return objectResult;
    }

    private static bool IsNon200Success(int? statusCode) =>
        statusCode is > StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices;

    private ObjectResult BuildProblem(Error error)
    {
        // An id already set on the Error wins; otherwise the request's id from Core.Logging.
        var correlationId = error.CorrelationId ?? correlationIdService.GetOrGenerate().Value;

        var problem = ApiProblemDetailsFactory.FromError(error, correlationId);

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" }
        };
    }
}


