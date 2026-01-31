using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.API.Filters;

/// <summary>
/// Wraps API responses in Result envelope.
/// For failed Results, converts Error to RFC 7807 ProblemDetails.
/// </summary>
public class SolResponseEnvelopeFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var resultValue = objectResult.Value;
            var resultValueType = resultValue?.GetType();

            // Handle failed Result<T>
            if (resultValueType is { IsGenericType: true } && resultValueType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var isSuccessProperty = resultValueType.GetProperty("IsSuccess");
                var errorProperty = resultValueType.GetProperty("Error");

                if (isSuccessProperty?.GetValue(resultValue) is false)
                {
                    if (errorProperty?.GetValue(resultValue) is Error error)
                    {
                        var statusCode = objectResult.StatusCode ?? StatusCodes.Status500InternalServerError;
                        context.Result = CreateProblemDetailsResult(error, statusCode, context.HttpContext.Request.Path);
                        return;
                    }
                }

                // Success case - already wrapped
                return;
            }

            // Handle failed non-generic Result
            if (resultValue is Result result)
            {
                if (result is { IsFailure: true, Error: not null })
                {
                    var statusCode = objectResult.StatusCode ?? StatusCodes.Status500InternalServerError;
                    context.Result = CreateProblemDetailsResult(result.Error, statusCode, context.HttpContext.Request.Path);
                }
                return;
            }

            // Wrap non-Result responses in Result envelope
            var responseStatusCode = objectResult.StatusCode ?? 200;
            var isSuccess = responseStatusCode >= 200 && responseStatusCode < 400;

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

    private static ObjectResult CreateProblemDetailsResult(Error error, int statusCode, string instance)
    {
        var (type, title) = GetTypeAndTitle(statusCode);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Type = type,
            Title = title,
            Detail = error.Message,
            Instance = instance
        };

        if (error.Description != null)
        {
            problemDetails.Extensions["description"] = error.Description;
        }
        if (error.Recoverable)
        {
            problemDetails.Extensions["recoverable"] = error.Recoverable;
        }

        return new ObjectResult(new
        {
            isSuccess = false,
            error = problemDetails
        })
        {
            StatusCode = statusCode
        };
    }

    private static (string Type, string Title) GetTypeAndTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ("https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
        StatusCodes.Status401Unauthorized => ("https://tools.ietf.org/html/rfc7235#section-3.1", "Unauthorized"),
        StatusCodes.Status403Forbidden => ("https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden"),
        StatusCodes.Status404NotFound => ("https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found"),
        StatusCodes.Status408RequestTimeout => ("https://tools.ietf.org/html/rfc7231#section-6.5.7", "Request Timeout"),
        StatusCodes.Status409Conflict => ("https://tools.ietf.org/html/rfc7231#section-6.5.8", "Conflict"),
        StatusCodes.Status422UnprocessableEntity => ("https://tools.ietf.org/html/rfc4918#section-11.2", "Unprocessable Entity"),
        499 => ("https://httpstatuses.com/499", "Client Closed Request"),
        StatusCodes.Status500InternalServerError => ("https://tools.ietf.org/html/rfc7231#section-6.6.1", "Internal Server Error"),
        StatusCodes.Status502BadGateway => ("https://tools.ietf.org/html/rfc7231#section-6.6.3", "Bad Gateway"),
        StatusCodes.Status503ServiceUnavailable => ("https://tools.ietf.org/html/rfc7231#section-6.6.4", "Service Unavailable"),
        StatusCodes.Status504GatewayTimeout => ("https://tools.ietf.org/html/rfc7231#section-6.6.5", "Gateway Timeout"),
        _ => ("https://tools.ietf.org/html/rfc7231", "Error")
    };

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Do nothing
    }
}
