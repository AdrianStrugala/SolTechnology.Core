using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FluentValidation;

namespace SolTechnology.Core.API.Filters;

/// <summary>
/// Global exception filter that catches unhandled exceptions and converts them to
/// RFC 7807 ProblemDetails responses wrapped in a Result envelope.
/// </summary>
public class ExceptionFilter(ILogger<ExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var problemDetails = MapExceptionToProblemDetails(context.Exception, context.HttpContext.Request.Path);

        logger.LogError(
            context.Exception,
            "Unhandled exception occurred. Title: [{Title}], Detail: [{Detail}]",
            problemDetails.Title,
            problemDetails.Detail);

        context.Result = new ObjectResult(new
        {
            isSuccess = false,
            error = problemDetails
        })
        {
            StatusCode = problemDetails.Status
        };

        context.ExceptionHandled = true;
    }

    private static ProblemDetails MapExceptionToProblemDetails(Exception exception, string instance)
    {
        return exception switch
        {
            ValidationException validationException => HandleValidationException(validationException, instance),
            ArgumentNullException argumentNullException => HandleArgumentNullException(argumentNullException, instance),
            ArgumentException argumentException => HandleArgumentException(argumentException, instance),
            UnauthorizedAccessException => HandleUnauthorizedAccessException(instance),
            TaskCanceledException => HandleTaskCanceledException(instance),
            OperationCanceledException => HandleOperationCanceledException(instance),
            KeyNotFoundException keyNotFoundException => HandleKeyNotFoundException(keyNotFoundException, instance),
            InvalidOperationException invalidOperationException => HandleInvalidOperationException(invalidOperationException, instance),
            _ => HandleUnknownException(exception, instance)
        };
    }

    private static ProblemDetails HandleValidationException(ValidationException exception, string instance)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => (object)g.Select(e => e.ErrorMessage).ToList());

        var problemDetails = CreateProblemDetails(
            StatusCodes.Status400BadRequest,
            "One or more validation errors occurred",
            instance);

        problemDetails.Extensions["source"] = "Validation";
        problemDetails.Extensions["description"] = exception.Message;

        foreach (var error in errors)
        {
            problemDetails.Extensions[error.Key] = error.Value;
        }

        return problemDetails;
    }

    private static ProblemDetails HandleArgumentNullException(ArgumentNullException exception, string instance)
    {
        var problemDetails = CreateProblemDetails(
            StatusCodes.Status400BadRequest,
            $"Required parameter [{exception.ParamName}] is missing",
            instance);

        problemDetails.Extensions["source"] = "Request";
        problemDetails.Extensions["parameterName"] = exception.ParamName ?? "unknown";

        return problemDetails;
    }

    private static ProblemDetails HandleArgumentException(ArgumentException exception, string instance)
    {
        var problemDetails = CreateProblemDetails(
            StatusCodes.Status400BadRequest,
            exception.Message,
            instance);

        problemDetails.Extensions["source"] = "Request";

        if (exception.ParamName != null)
        {
            problemDetails.Extensions["parameterName"] = exception.ParamName;
        }

        return problemDetails;
    }

    private static ProblemDetails HandleUnauthorizedAccessException(string instance)
    {
        var problemDetails = CreateProblemDetails(
            StatusCodes.Status401Unauthorized,
            "Access denied",
            instance);

        problemDetails.Extensions["source"] = "Authentication";

        return problemDetails;
    }

    private static ProblemDetails HandleOperationCanceledException(string instance)
    {
        var problemDetails = CreateProblemDetails(
            StatusCodes.Status408RequestTimeout,
            "The request was cancelled or timed out",
            instance);

        problemDetails.Extensions["source"] = "Request";
        problemDetails.Extensions["recoverable"] = true;

        return problemDetails;
    }

    private static ProblemDetails HandleTaskCanceledException(string instance)
    {
        var problemDetails = CreateProblemDetails(
            499,
            "The client closed the request before the server could respond",
            instance);

        problemDetails.Extensions["source"] = "Request";
        problemDetails.Extensions["recoverable"] = true;

        return problemDetails;
    }

    private static ProblemDetails HandleKeyNotFoundException(KeyNotFoundException exception, string instance)
    {
        var problemDetails = CreateProblemDetails(
            StatusCodes.Status404NotFound,
            exception.Message,
            instance);

        problemDetails.Extensions["source"] = "Request";

        return problemDetails;
    }

    private static ProblemDetails HandleInvalidOperationException(InvalidOperationException exception, string instance)
    {
        var problemDetails = CreateProblemDetails(
            StatusCodes.Status409Conflict,
            exception.Message,
            instance);

        problemDetails.Extensions["source"] = "Request";

        return problemDetails;
    }

    private static ProblemDetails HandleUnknownException(Exception exception, string instance)
    {
        var problemDetails = CreateProblemDetails(
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred. Please try again later.",
            instance);

        problemDetails.Extensions["source"] = exception.Source ?? "Server";
        problemDetails.Extensions["exceptionType"] = exception.GetType().Name;

#if DEBUG
        problemDetails.Extensions["stackTrace"] = exception.StackTrace ?? string.Empty;
#endif

        return problemDetails;
    }

    private static ProblemDetails CreateProblemDetails(int statusCode, string detail, string instance)
    {
        var (type, title) = GetTypeAndTitle(statusCode);

        return new ProblemDetails
        {
            Status = statusCode,
            Type = type,
            Title = title,
            Detail = detail,
            Instance = instance
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
}
