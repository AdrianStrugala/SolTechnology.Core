using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using FluentValidation;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.API.Filters;

/// <summary>
/// Global exception filter that catches unhandled exceptions and converts them to structured API responses.
/// </summary>
[Obsolete("Use ProblemDetailsExceptionHandler instead. Register with services.AddProblemDetailsExceptionHandler() and app.UseExceptionHandler(). See RFC 7807 for ProblemDetails standard.")]
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

        _logger.LogError(
            context.Exception,
            "Unhandled exception occurred. Source: [{Source}], Message: [{Message}]",
            error.Source,
            error.Message);

        context.Result = new ObjectResult(new Result
        {
            Error = error,
            IsSuccess = false
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
            OperationCanceledException => HandleTaskCanceledException(),
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
            details: new Dictionary<string, object> { ["ParameterName"] = exception.ParamName ?? "unknown" });

        return (HttpStatusCode.BadRequest, error);
    }

    private static (HttpStatusCode, Error) HandleArgumentException(ArgumentException exception)
    {
        var error = Error.Validation(
            source: "Request",
            message: exception.Message,
            details: exception.ParamName != null
                ? new Dictionary<string, object> { ["ParameterName"] = exception.ParamName }
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
            message: "The request was cancelled");

        // 499 is nginx's "Client Closed Request" status code
        return ((HttpStatusCode)499, error);
    }

    private static (HttpStatusCode, Error) HandleUnknownException(Exception exception)
    {
        var error = Error.Internal(
            source: exception.Source ?? "Server",
            message: "An unexpected error occurred. Please try again later.",
            details: new Dictionary<string, object>
            {
                ["ExceptionType"] = exception.GetType().Name
            });

        error.Description = exception.StackTrace;

        return (HttpStatusCode.InternalServerError, error);
    }
}
