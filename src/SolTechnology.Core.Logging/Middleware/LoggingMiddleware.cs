using System.Text.Json;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;
namespace SolTechnology.Core.Logging.Middleware;
/// <summary>
/// Request-scoped middleware that:
/// <list type="bullet">
///   <item>Reads / generates the <see cref="CorrelationId"/> (W3C trace id from <see cref="System.Diagnostics.Activity"/>).</item>
///   <item>Pushes the correlation into the logger scope so every log event in the request carries it.</item>
///   <item>Invokes any registered <see cref="ILogScopeEnricher"/>s and folds their properties into the same scope.</item>
///   <item>Echoes the correlation back on the response.</item>
///   <item>Logs request start / finish with status-code-aware log levels.</item>
///   <item>Demotes <see cref="ConnectionResetException"/> to <see cref="LogLevel.Warning"/>
///         (client aborts are not server faults).</item>
/// </list>
/// Does <b>not</b> read the request body or query string itself. Application-specific
/// enrichment (user id from claims, business field from body, etc.) is the responsibility
/// of an <see cref="ILogScopeEnricher"/> implementation registered in DI.
/// </summary>
public sealed class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LoggingOptions _options;
    public LoggingMiddleware(RequestDelegate next, IOptions<LoggingOptions>? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? new LoggingOptions();
    }
    public async Task InvokeAsync(
        HttpContext context,
        ILogger<LoggingMiddleware> logger,
        ICorrelationIdService correlationIdService,
        IEnumerable<ILogScopeEnricher> enrichers)
    {
        ArgumentNullException.ThrowIfNull(context);
        var correlation = CorrelationId.FromRequest(context.Request, out var headerError);
        correlationIdService.Set(correlation);
        // Echo correlation back on the response.
        context.Response.OnStarting(static state =>
        {
            var (httpContext, corr) = ((HttpContext, CorrelationId))state;
            corr.EnrichResponse(httpContext.Response);
            return Task.CompletedTask;
        }, (context, correlation));

        // Skip noisy paths (health checks, liveness probes, swagger). Correlation is still set
        // and echoed; we just don't emit the request envelope logs and skip enrichers entirely.
        if (ShouldSkipPath(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var stopwatch = ValueStopwatch.StartNew();
        // Build the scope dictionary: correlation first, then per-app enrichers fold in.
        var scopeDictionary = correlation.GetScope();

        // Snapshot once - we iterate the enricher sequence twice (body-prepare scan + Enrich loop).
        var enricherList = enrichers as IList<ILogScopeEnricher> ?? enrichers.ToArray();

        // If any enricher needs the JSON body, async-buffer + parse it once before invoking
        // the (synchronous) Enrich loop. Body is reset so MVC model binding still works.
        await PrepareJsonBodyIfRequestedAsync(context, enricherList, logger).ConfigureAwait(false);

        foreach (var enricher in enricherList)
        {
            try
            {
                enricher.Enrich(context, scopeDictionary);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Log scope enricher [{EnricherType}] threw and was skipped",
                    enricher.GetType().FullName);
            }
        }
        // Single ambient scope for the lifetime of the request.
        using var scope = logger.BeginScope(scopeDictionary);
        if (headerError is not null && _options.LogClientCorrelationParseErrors)
        {
            logger.LogWarning(
                "Invalid [{Header}] header rejected: [{CorrelationError}]",
                CorrelationId.HeaderKey,
                headerError);
        }
        logger.LogInformation(
            "Started request [{RequestMethod}] [{RequestPath}]",
            context.Request.Method,
            context.Request.Path);
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsClientAbort(ex, context))
        {
            // Client closed the connection - not a server fault.
            logger.LogWarning(
                ex,
                "Request [{RequestMethod}] [{RequestPath}] aborted by client after [{ElapsedMs} ms]",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
            logger.LogError(
                ex,
                "Request [{RequestMethod}] [{RequestPath}] failed after [{ElapsedMs} ms]",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            var statusCode = context.Response.StatusCode;
            var level = statusCode >= 500 ? LogLevel.Error
                      : statusCode >= 400 ? LogLevel.Warning
                                          : LogLevel.Information;
            logger.Log(
                level,
                "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
    private async Task PrepareJsonBodyIfRequestedAsync(
        HttpContext context,
        IList<ILogScopeEnricher> enrichers,
        ILogger logger)
    {
        var requested = false;
        foreach (var enricher in enrichers)
        {
            if (enricher is IRequiresRequestBody bodyAware && bodyAware.RequiresBody(context))
            {
                requested = true;
                break;
            }
        }
        if (!requested)
        {
            return;
        }

        var request = context.Request;

        if (request.ContentType is null ||
            !request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (request.ContentLength is null || request.ContentLength <= 0 ||
            request.ContentLength > _options.MaxLoggedJsonBodyBytes)
        {
            return;
        }

        try
        {
            request.EnableBuffering();
            request.Body.Position = 0;

            var document = await JsonDocument
                .ParseAsync(request.Body, default, context.RequestAborted)
                .ConfigureAwait(false);

            context.Items[LogDetailEnricher.ParsedBodyItemKey] = document;
            context.Response.RegisterForDispose(document);
        }
        catch (JsonException ex)
        {
            // Malformed body is the caller's problem, not ours - downstream model binding
            // will surface it as a 400. Just skip enrichment.
            logger.LogDebug(
                ex,
                "Skipping body-based log enrichment for [{RequestPath}] - JSON could not be parsed",
                request.Path);
        }
        catch (OperationCanceledException)
        {
            // Client gone, nothing to enrich. Don't surface as an error.
        }
        finally
        {
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }
        }
    }

    private bool ShouldSkipPath(PathString path)
    {
        if (_options.SkipPaths is null || _options.SkipPaths.Count == 0)
        {
            return false;
        }

        var value = path.Value;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        for (var i = 0; i < _options.SkipPaths.Count; i++)
        {
            var prefix = _options.SkipPaths[i];
            if (!string.IsNullOrEmpty(prefix) &&
                value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Decides whether the exception thrown from the request pipeline represents the client
    /// disconnecting (which we want to log at <see cref="LogLevel.Warning"/>) rather than a
    /// genuine server fault (which must surface at <see cref="LogLevel.Error"/>).
    /// </summary>
    /// <remarks>
    /// <see cref="OperationCanceledException"/> alone is ambiguous — it can come from a SQL
    /// timeout, a custom <see cref="CancellationTokenSource"/>, a test, etc. Only ASP.NET Core's
    /// <see cref="HttpContext.RequestAborted"/> token signals an actual client disconnect.
    /// </remarks>
    private static bool IsClientAbort(Exception exception, HttpContext context)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is ConnectionResetException)
            {
                return true;
            }

            if (current is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                return true;
            }
        }
        return false;
    }
}
