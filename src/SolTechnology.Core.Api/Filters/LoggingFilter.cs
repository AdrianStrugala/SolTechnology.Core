using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging;
using System.Diagnostics;
using System.Text;

namespace SolTechnology.Core.Api.Filters;

public class LoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<LoggingFilter> _logger;

    public LoggingFilter(ILogger<LoggingFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //TODO: check if that's really the nice way to do this. How to be sure, that result is success or not?

        context.HttpContext.Request.EnableBuffering();

        var request = context.ActionArguments.FirstOrDefault().Value;

        if (request is ILoggableOperation loggableRequest)
        {
            using var scope = _logger.BeginOperationScope(new KeyValuePair<string, object>(
                loggableRequest.LogScope.OperationIdName,
                loggableRequest.LogScope.OperationId));

            var sw = Stopwatch.StartNew();
            var operationName = loggableRequest.LogScope.OperationName;
            _logger.OperationStarted(operationName);


            var resultContext = await next();


            //TODO: this won't work. It's called before ResultFilter. Probably better place is Exception and Envelope filters. Add operaiton name to scope

            if (resultContext.Result is ObjectResult objectResult)
            {
                var value = objectResult.Value;
                var valueType = value?.GetType();
                if (valueType is { IsGenericType: true } &&
                    valueType.GetGenericTypeDefinition() == typeof(ResponseEnvelope<>))
                {
                    var isSuccess =
                        (bool)valueType.GetProperty(nameof(ResponseEnvelope<object>.IsSuccess))?.GetValue(value)!;

                    if (isSuccess)
                    {
                        _logger.OperationSucceeded(operationName, sw.ElapsedMilliseconds);
                    }
                    else
                    {
                        _logger.OperationFailed(operationName, sw.ElapsedMilliseconds);
                    }
                }
            }
        }
        else
        {
            await next();
        }
    }

    private async Task<ILoggableOperation> ReadRequestBodyAsync(HttpRequest request)
    {
        request.Body.Position = 0;

        try
        {
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
            {
                var bodyAsText = await reader.ReadToEndAsync();

                var result =
                    await System.Text.Json.JsonSerializer.DeserializeAsync<LoggableOperation>(request.Body);
                return result;
            }
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
}