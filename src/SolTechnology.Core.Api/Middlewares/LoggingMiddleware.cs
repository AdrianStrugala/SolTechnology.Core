using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using System.Text;
using System.Text.Json;

namespace SolTechnology.Core.Api.Middlewares;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
        var asyncStopwatch = new AsyncStopwatch();

        using (AddRequestIdsToScope(context))
        {
            _logger.LogInformation("Started request: [{RequestMethod} {RequestPath}]",
                context.Request.Method, context.Request.Path);

            try
            {
                await _next(context).ConfigureAwait(false);
            }
            finally
            {
                _logger.LogInformation("Finished request in [{ElapsedMilliseconds}] ms with status code [{StatusCode}]",
                    asyncStopwatch.Elapsed.TotalMilliseconds, context.Response.StatusCode);
            }

        }

    }

    private IDisposable AddRequestIdsToScope(HttpContext context)
    {
        //To show how can some id's be extracted from query
        context.Request.Query.TryGetValue("userId", out var userId);

        //To show how can some id's be extracted from body
        context.Request.EnableBuffering();
        var body = ReadRequestBody(context.Request);

        string? requestId = null;
        if (body is not null)
        {
            if ((bool)body?.TryGetProperty("requestId", out var property))
            {
                requestId = property?.ToString();
            }
        }


        var disposable = new DisposableCollection
        {
            _logger.AddToScope("environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")),
            _logger.AddToScope("correlationId", Guid.NewGuid().ToString()),
            _logger.AddToScope("userId", userId.ToString() ?? "unknown"),
            _logger.AddToScope("requestId", requestId ?? "unknown"),
        };

        return disposable;
    }

    private JsonElement? ReadRequestBody(HttpRequest request)
    {
        request.Body.Position = 0;

        try
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var bodyAsText = reader.ReadToEnd();
            using var document = JsonDocument.Parse(bodyAsText);
            var root = document.RootElement;
            return root;
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