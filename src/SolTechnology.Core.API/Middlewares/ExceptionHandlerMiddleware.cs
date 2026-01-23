using Microsoft.AspNetCore.Http;
using System.Net;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.API.Middlewares;

[Obsolete("FILTER is preferred")]
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ILogger<ExceptionHandlerMiddleware> logger)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var response = context.Response;
            response.StatusCode = GetStatusCode(exception);

            var responseEnvelope = new Result
            {
                Error = Error.From(exception),
                IsSuccess = false
            };

            logger.LogError(exception.Message);

            await response.WriteAsJsonAsync(responseEnvelope);
        }
    }

    public int GetStatusCode(Exception exception)
    {
        int code;
        switch (exception)
        {
            case TaskCanceledException:
                code = 499;
                break;
            default:
                code = (int)HttpStatusCode.BadRequest;
                break;
        }
        return code;
    }
}
