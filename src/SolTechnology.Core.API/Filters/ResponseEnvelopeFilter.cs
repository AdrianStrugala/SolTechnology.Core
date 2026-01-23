using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.API.Filters;

/// <summary>
/// Wraps API responses in Result envelope.
/// </summary>
[Obsolete("Use ProblemDetails for error responses instead (RFC 7807). Controllers should return domain objects directly without wrapping. Register ProblemDetailsExceptionHandler for consistent error handling.")]
public class ResponseEnvelopeFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var resultValue = objectResult.Value;
            var resultValueType = resultValue?.GetType();

            if (resultValueType is { IsGenericType: true } && resultValueType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                return;
            }

            if (resultValue is Result)
            {
                return;
            }

            var statusCode = objectResult.StatusCode ?? 200;
            var isSuccess = statusCode >= 200 && statusCode < 400;

            context.Result = new ObjectResult(isSuccess
                ? new Result<object?>
                {
                    Data = resultValue,
                    IsSuccess = true
                }
                : Result<object?>.Fail(resultValue?.ToString() ?? "An error occurred"))
            {
                StatusCode = objectResult.StatusCode
            };
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Do nothing
    }
}