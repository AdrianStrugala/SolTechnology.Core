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
public sealed class LoggingMiddleware(RequestDelegate next, IOptions<LoggingOptions>? options = null)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly LoggingOptions _options = options?.Value ?? new LoggingOptions();

    public async Task InvokeAsync(
        HttpContext context,
        ILogger<LoggingMiddleware> logger,
        ICorrelationIdService correlationIdService,
        IEnumerable<ILogScopeEnricher> enrichers,
        ITimingService timingService)
    {
        ArgumentNullException.ThrowIfNull(context);
        var correlation = CorrelationId.FromRequest(context.Request, out var headerError);
        correlationIdService.Set(correlation);
        context.Response.OnStarting(static state =>
        {
            var (httpContext, corr) = ((HttpContext, CorrelationId))state;
            corr.EnrichResponse(httpContext.Response);
            return Task.CompletedTask;
        }, (context, correlation));

        // Noisy paths (health, swagger) skip envelope logs + enrichers; correlation is still echoed.
        if (LoggingPathMatcher.MatchesAny(context.Request.Path, _options.SkipPaths))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var stopwatch = ValueStopwatch.StartNew();
        timingService.Reset();
        var scopeProperties = await BuildScope(context, correlation, enrichers, logger).ConfigureAwait(false);
        using var scope = logger.BeginScope(scopeProperties);
        if (headerError is not null && _options.LogClientCorrelationParseErrors)
        {
            RequestEnvelopeLogger.CorrelationHeaderRejected(logger, CorrelationId.HeaderKey, headerError);
        }

        RequestEnvelopeLogger.RequestStarted(logger, context.Request.Method, context.Request.Path);
        await InvokeNextAndLogOutcome(context, logger, timingService, stopwatch).ConfigureAwait(false);
    }

    private async Task<IDictionary<string, object?>> BuildScope(
        HttpContext context,
        CorrelationId correlation,
        IEnumerable<ILogScopeEnricher> enrichers,
        ILogger logger)
    {
        var scopeProperties = correlation.GetScope();

        // Snapshot: the enricher sequence is iterated twice (body-prepare + Enrich).
        var enricherList = enrichers as IList<ILogScopeEnricher> ?? enrichers.ToArray();
        await RequestBodyBuffer.ParseJsonIfRequested(context, enricherList, _options.MaxLoggedJsonBodyBytes, logger)
            .ConfigureAwait(false);

        foreach (var enricher in enricherList)
        {
            try
            {
                enricher.Enrich(context, scopeProperties);
            }
            catch (Exception ex)
            {
                RequestEnvelopeLogger.EnricherFailed(logger, ex, enricher.GetType().FullName);
            }
        }

        return scopeProperties;
    }

    private async Task InvokeNextAndLogOutcome(
        HttpContext context,
        ILogger logger,
        ITimingService timingService,
        ValueStopwatch stopwatch)
    {
        var request = context.Request;
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsClientAbort(ex, context))
        {
            RequestEnvelopeLogger.RequestAborted(logger, ex, request.Method, request.Path, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            RequestEnvelopeLogger.RequestFailed(logger, ex, request.Method, request.Path, stopwatch.ElapsedMilliseconds);
            throw;
        }

        // Only reached without an exception: a logged failure plus Finished would double-count error rates.
        RequestEnvelopeLogger.RequestFinished(logger, context, timingService, stopwatch.ElapsedMilliseconds);
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
