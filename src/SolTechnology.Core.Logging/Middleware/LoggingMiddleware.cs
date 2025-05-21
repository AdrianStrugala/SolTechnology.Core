using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private ILogger<LoggingMiddleware> _logger = null!;

    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
        var asyncStopwatch = new AsyncStopwatch();

        //cannot put await to using. It causes logger scope to be disposed
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

        //To show how can some id's be extracted from route
        // context.Request.RouteValues.TryGetValue("category", out var category);

        //To show how can some id's be extracted from body
        var body = ReadRequestBody(context.Request);

        string? name = null;
        if (body is not null)
        {
            if (body.RootElement.TryGetProperty("name", out var nameElement))
            {
                name = nameElement.ToString();
            }
        }


        var disposable = new DisposableCollection
        {
            _logger.AddToScope("environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"),
            _logger.AddToScope("correlationId", Guid.NewGuid().ToString()),
            _logger.AddToScope("userId", userId.ToString() ?? "unknown"),
            // _logger.AddToScope("category", category?.ToString() ?? "unknown"),
            _logger.AddToScope("name", name ?? "unknown"),
        };

        return disposable;
    }

    private JsonDocument? ReadRequestBody(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        try
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var bodyAsText = reader.ReadToEnd();
            var document = JsonDocument.Parse(bodyAsText);
            return document;
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