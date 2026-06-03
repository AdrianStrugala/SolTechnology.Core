using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.API.Filters;

/// <summary>
/// Converts <c>Result</c> / <c>Result&lt;T&gt;</c> values returned from controller actions into
/// the wire-format expected by HTTP clients:
/// <list type="bullet">
///   <item>Successful <c>Result&lt;T&gt;</c> → HTTP 200 with <c>Data</c> as the raw body.</item>
///   <item>Successful non-generic <c>Result</c> → HTTP 204 No Content.</item>
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
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: { } value })
        {
            // Dispatch is fully static: Result covers both Result and Result<T> (Data is exposed
            // through the virtual GetData() — null on the base, payload on the generic). A bare
            // Error catches the BadRequest(error) shorthand some handlers still use. No
            // reflection — AOT/trim safe, no PropertyInfo lookups per request.
            if (value is Result result)
            {
                context.Result = Convert(result);
            }
            else if (value is Error error)
            {
                context.Result = BuildProblem(error);
            }
        }

        return next();
    }

    private IActionResult Convert(Result result)
    {
        if (!result.IsSuccess)
        {
            return BuildProblem(result.Error ?? new Error { Message = "Operation failed." });
        }

        // Success: GetData() returns the payload for Result<T> or null for the non-generic
        // base. Null payload → 204 No Content (no body). See Result.GetData() remarks.
        var data = result.GetData();
        return data is null
            ? new StatusCodeResult(StatusCodes.Status204NoContent)
            : new ObjectResult(data) { StatusCode = StatusCodes.Status200OK };
    }

    private ObjectResult BuildProblem(Error error)
    {
        // Source of truth for correlation id is Core.Logging — same value as the
        // X-Correlation-Id response header and the CorrelationId log scope property. We only
        // generate one if the caller did not already populate it on the Error.
        var correlationId = error.CorrelationId ?? correlationIdService.GetOrGenerate().Value;

        var problem = ApiProblemDetailsFactory.FromError(error, correlationId);

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" }
        };
    }
}


