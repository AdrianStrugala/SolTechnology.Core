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
public sealed class ResultConversionFilter : IAsyncResultFilter
{
    private readonly ICorrelationIdService _correlationIdService;

    public ResultConversionFilter(ICorrelationIdService correlationIdService)
    {
        _correlationIdService = correlationIdService;
    }

    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: { } value })
        {
            // Order matters: Result<T> is a Result, so the generic check must come first. A
            // bare Error (BadRequest(error)) is a separate case — handlers occasionally surface
            // an Error directly without wrapping in Result.
            if (value is Result baseResult)
            {
                context.Result = ConvertResult(baseResult, value);
            }
            else if (value is Error error)
            {
                context.Result = BuildProblem(error);
            }
        }

        return next();
    }

    private IActionResult ConvertResult(Result baseResult, object originalValue)
    {
        if (baseResult.IsSuccess)
        {
            // Result<T> carries a Data property; non-generic Result does not. Reflection here
            // is bound to the public property and only runs on the success path of an MVC action,
            // i.e. once per request. JIT optimization makes this negligible vs. the JSON write.
            var dataProperty = originalValue.GetType().GetProperty(nameof(Result<object>.Data));

            if (dataProperty is null)
            {
                // Non-generic Result success → 204 No Content. Body intentionally absent.
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            var data = dataProperty.GetValue(originalValue);
            return new ObjectResult(data) { StatusCode = StatusCodes.Status200OK };
        }

        // Failed result. Error must be present by Result contract; fall back to a placeholder
        // so we still emit a valid ProblemDetails if a caller built a malformed instance.
        var error = baseResult.Error ?? new Error { Message = "Operation failed." };
        return BuildProblem(error);
    }

    private ObjectResult BuildProblem(Error error)
    {
        // Source of truth for correlation id is Core.Logging — same value as the
        // X-Correlation-Id response header and the CorrelationId log scope property. We only
        // generate one if the caller did not already populate it on the Error.
        var correlationId = error.CorrelationId ?? _correlationIdService.GetOrGenerate().Value;

        var problem = ApiProblemDetailsFactory.FromError(error, correlationId);

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" }
        };
    }
}


