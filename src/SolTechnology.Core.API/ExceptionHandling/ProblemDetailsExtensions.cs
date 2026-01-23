using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.API.ExceptionHandling;

/// <summary>
/// Extension methods for ProblemDetails configuration and conversion.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Adds the ProblemDetails exception handler to the service collection.
    /// </summary>
    public static IServiceCollection AddProblemDetailsExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    /// <summary>
    /// Converts an Error object to a ProblemDetails response.
    /// </summary>
    public static ProblemDetails ToProblemDetails(this Error error, string? instance = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = (int)error.StatusCode,
            Title = GetTitleFromStatusCode((int)error.StatusCode),
            Type = GetTypeFromStatusCode((int)error.StatusCode),
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

    /// <summary>
    /// Creates an IResult that returns a ProblemDetails response from an Error.
    /// </summary>
    public static IResult ToProblemDetailsResult(this Error error, HttpContext httpContext)
    {
        var problemDetails = error.ToProblemDetails(httpContext.Request.Path);
        return Results.Problem(problemDetails);
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
        StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        StatusCodes.Status502BadGateway => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
        StatusCodes.Status503ServiceUnavailable => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
        StatusCodes.Status504GatewayTimeout => "https://tools.ietf.org/html/rfc7231#section-6.6.5",
        _ => "https://tools.ietf.org/html/rfc7231"
    };
}
