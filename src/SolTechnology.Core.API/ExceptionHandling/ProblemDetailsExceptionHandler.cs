using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.API.ExceptionHandling;

/// <summary>
/// Global exception handler that converts exceptions to RFC 7807 ProblemDetails responses.
/// </summary>
public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = MapExceptionToProblemDetails(exception, httpContext);

        logger.LogError(
            exception,
            "Exception occurred: [{ExceptionType}] [{Message}]",
            exception.GetType().Name,
            exception.Message);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, ProblemDetails ProblemDetails) MapExceptionToProblemDetails(
        Exception exception,
        HttpContext httpContext)
    {
        return exception switch
        {
            ValidationException validationException => HandleValidationException(validationException, httpContext),
            ArgumentNullException argumentNullException => HandleArgumentNullException(argumentNullException, httpContext),
            ArgumentException argumentException => HandleArgumentException(argumentException, httpContext),
            UnauthorizedAccessException => HandleUnauthorizedAccessException(httpContext),
            TaskCanceledException => HandleTaskCanceledException(httpContext),
            OperationCanceledException => HandleOperationCanceledException(httpContext),
            KeyNotFoundException keyNotFoundException => HandleKeyNotFoundException(keyNotFoundException, httpContext),
            InvalidOperationException invalidOperationException => HandleInvalidOperationException(invalidOperationException, httpContext),
            _ => HandleUnknownException(exception, httpContext)
        };
    }

    private static (int, ProblemDetails) HandleValidationException(ValidationException exception, HttpContext httpContext)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Detail = "One or more validation errors occurred.",
            Instance = httpContext.Request.Path
        };

        return (StatusCodes.Status400BadRequest, problemDetails);
    }

    private static (int, ProblemDetails) HandleArgumentNullException(ArgumentNullException exception, HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Detail = $"Required parameter [{exception.ParamName}] is missing.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["parameterName"] = exception.ParamName;

        return (StatusCodes.Status400BadRequest, problemDetails);
    }

    private static (int, ProblemDetails) HandleArgumentException(ArgumentException exception, HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception.ParamName != null)
        {
            problemDetails.Extensions["parameterName"] = exception.ParamName;
        }

        return (StatusCodes.Status400BadRequest, problemDetails);
    }

    private static (int, ProblemDetails) HandleUnauthorizedAccessException(HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Detail = "Access denied.",
            Instance = httpContext.Request.Path
        };

        return (StatusCodes.Status401Unauthorized, problemDetails);
    }

    private static (int, ProblemDetails) HandleOperationCanceledException(HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status408RequestTimeout,
            Title = "Request Timeout",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7",
            Detail = "The request was cancelled or timed out.",
            Instance = httpContext.Request.Path
        };

        return (StatusCodes.Status408RequestTimeout, problemDetails);
    }

    private static (int, ProblemDetails) HandleTaskCanceledException(HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Status = 499, // Client Closed Request
            Title = "Client Closed Request",
            Type = "https://httpstatuses.com/499",
            Detail = "The client closed the request before the server could respond.",
            Instance = httpContext.Request.Path
        };

        return (499, problemDetails);
    }

    private static (int, ProblemDetails) HandleKeyNotFoundException(KeyNotFoundException exception, HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not Found",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        return (StatusCodes.Status404NotFound, problemDetails);
    }

    private static (int, ProblemDetails) HandleInvalidOperationException(InvalidOperationException exception, HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        return (StatusCodes.Status409Conflict, problemDetails);
    }

    private static (int, ProblemDetails) HandleUnknownException(Exception exception, HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Detail = "An unexpected error occurred. Please try again later.",
            Instance = httpContext.Request.Path
        };

#if DEBUG
        problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
        problemDetails.Extensions["exceptionMessage"] = exception.Message;
        problemDetails.Extensions["stackTrace"] = exception.StackTrace;
#endif

        return (StatusCodes.Status500InternalServerError, problemDetails);
    }
}
