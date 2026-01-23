using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using FluentValidation;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.API.Filters;

/// <summary>
/// Global exception filter that catches unhandled exceptions and converts them to
/// RFC 7807 ProblemDetails responses wrapped in a Result envelope.
/// </summary>
public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var (statusCode, error) = HandleException(context.Exception);
        var problemDetails = error.ToProblemDetails(context.HttpContext.Request.Path);

        _logger.LogError(
            context.Exception,
            "Unhandled exception occurred. Source: [{Source}], Message: [{Message}]",
            error.Source,
            error.Message);

        context.Result = new ObjectResult(new
        {
            isSuccess = false,
            error = problemDetails
        })
        {
            StatusCode = (int)statusCode
        };

        context.ExceptionHandled = true;
    }

    /// <summary>
    /// Maps exceptions to appropriate HTTP status codes and Error objects.
    /// </summary>
    public (HttpStatusCode, Error) HandleException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => HandleValidationException(validationException),
            ArgumentNullException argumentNullException => HandleArgumentNullException(argumentNullException),
            ArgumentException argumentException => HandleArgumentException(argumentException),
            UnauthorizedAccessException => HandleUnauthorizedAccessException(),
            TaskCanceledException => HandleTaskCanceledException(),
            OperationCanceledException => HandleOperationCanceledException(),
            KeyNotFoundException keyNotFoundException => HandleKeyNotFoundException(keyNotFoundException),
            InvalidOperationException invalidOperationException => HandleInvalidOperationException(invalidOperationException),
            _ => HandleUnknownException(exception)
        };
    }

    private static (HttpStatusCode, Error) HandleValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => (object)g.Select(e => e.ErrorMessage).ToList());

        var error = Error.Validation(
            source: "Validation",
            message: "One or more validation errors occurred",
            details: errors);

        error.Description = exception.Message;

        return (HttpStatusCode.BadRequest, error);
    }

    private static (HttpStatusCode, Error) HandleArgumentNullException(ArgumentNullException exception)
    {
        var error = Error.Validation(
            source: "Request",
            message: $"Required parameter [{exception.ParamName}] is missing",
            details: new Dictionary<string, object> { ["parameterName"] = exception.ParamName ?? "unknown" });

        return (HttpStatusCode.BadRequest, error);
    }

    private static (HttpStatusCode, Error) HandleArgumentException(ArgumentException exception)
    {
        var error = Error.Validation(
            source: "Request",
            message: exception.Message,
            details: exception.ParamName != null
                ? new Dictionary<string, object> { ["parameterName"] = exception.ParamName }
                : null);

        return (HttpStatusCode.BadRequest, error);
    }

    private static (HttpStatusCode, Error) HandleUnauthorizedAccessException()
    {
        var error = Error.Unauthorized(
            source: "Authentication",
            message: "Access denied");

        return (HttpStatusCode.Unauthorized, error);
    }

    private static (HttpStatusCode, Error) HandleOperationCanceledException()
    {
        var error = Error.Timeout(
            source: "Request",
            message: "The request was cancelled or timed out");

        return (HttpStatusCode.RequestTimeout, error);
    }

    private static (HttpStatusCode, Error) HandleTaskCanceledException()
    {
        var error = Error.Timeout(
            source: "Request",
            message: "The client closed the request before the server could respond");
        error.StatusCode = (HttpStatusCode)499;

        return ((HttpStatusCode)499, error);
    }

    private static (HttpStatusCode, Error) HandleKeyNotFoundException(KeyNotFoundException exception)
    {
        var error = Error.NotFound(
            source: "Request",
            message: exception.Message);

        return (HttpStatusCode.NotFound, error);
    }

    private static (HttpStatusCode, Error) HandleInvalidOperationException(InvalidOperationException exception)
    {
        var error = new Error
        {
            Source = "Request",
            Message = exception.Message,
            StatusCode = HttpStatusCode.Conflict,
            Recoverable = false
        };

        return (HttpStatusCode.Conflict, error);
    }

    private static (HttpStatusCode, Error) HandleUnknownException(Exception exception)
    {
        var error = Error.Internal(
            source: exception.Source ?? "Server",
            message: "An unexpected error occurred. Please try again later.",
            details: new Dictionary<string, object>
            {
                ["exceptionType"] = exception.GetType().Name
            });

        error.Description = exception.StackTrace;

        return (HttpStatusCode.InternalServerError, error);
    }
}

/// <summary>
/// Extension methods for converting Error to ProblemDetails.
/// </summary>
public static class ErrorToProblemDetailsExtensions
{
    /// <summary>
    /// Converts an Error object to a ProblemDetails response.
    /// </summary>
    public static ProblemDetails ToProblemDetails(this Error error, string? instance = null)
    {
        var statusCode = (int)error.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitleFromStatusCode(statusCode),
            Type = GetTypeFromStatusCode(statusCode),
            Detail = error.Message,
            Instance = instance
        };

        if (!string.IsNullOrEmpty(error.Source))
        {
            problemDetails.Extensions["source"] = error.Source;
        }

        if (!string.IsNullOrEmpty(error.Description))
        {
            problemDetails.Extensions["description"] = error.Description;
        }

        if (error.Details != null && error.Details.Count > 0)
        {
            foreach (var detail in error.Details)
            {
                problemDetails.Extensions[detail.Key] = detail.Value;
            }
        }

        problemDetails.Extensions["recoverable"] = error.Recoverable;

        return problemDetails;
    }

    private static string GetTitleFromStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status408RequestTimeout => "Request Timeout",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        499 => "Client Closed Request",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        StatusCodes.Status502BadGateway => "Bad Gateway",
        StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
        StatusCodes.Status504GatewayTimeout => "Gateway Timeout",
        _ => "Error"
    };

    private static string GetTypeFromStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
        StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        StatusCodes.Status408RequestTimeout => "https://tools.ietf.org/html/rfc7231#section-6.5.7",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        StatusCodes.Status422UnprocessableEntity => "https://tools.ietf.org/html/rfc4918#section-11.2",
        499 => "https://httpstatuses.com/499",
        StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        StatusCodes.Status502BadGateway => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
        StatusCodes.Status503ServiceUnavailable => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
        StatusCodes.Status504GatewayTimeout => "https://tools.ietf.org/html/rfc7231#section-6.6.5",
        _ => "https://tools.ietf.org/html/rfc7231"
    };
}
