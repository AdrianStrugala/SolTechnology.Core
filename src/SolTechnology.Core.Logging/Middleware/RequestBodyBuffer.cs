using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging.Enrichment;

namespace SolTechnology.Core.Logging.Middleware;

/// <summary>
/// Buffers and parses the JSON request body once when an <see cref="IRequiresRequestBody"/> enricher
/// asks for it, caches the document for <see cref="LogDetailEnricher"/>, and rewinds the stream so
/// model binding still reads the body.
/// </summary>
internal static class RequestBodyBuffer
{
    public static async Task ParseJsonIfRequested(
        HttpContext context,
        IList<ILogScopeEnricher> enrichers,
        long maxBodyBytes,
        ILogger logger)
    {
        var request = context.Request;
        if (!IsRequested(context, enrichers) || !IsBufferableJson(request, maxBodyBytes))
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
            // Malformed body - model binding will surface a 400; skip enrichment.
            logger.LogDebug(
                ex,
                "Skipping body-based log enrichment for [{RequestPath}] - JSON could not be parsed",
                request.Path);
        }
        catch (OperationCanceledException)
        {
            // Client gone - nothing to enrich.
        }
        finally
        {
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }
        }
    }

    private static bool IsRequested(HttpContext context, IList<ILogScopeEnricher> enrichers)
    {
        foreach (var enricher in enrichers)
        {
            if (enricher is IRequiresRequestBody bodyAware && bodyAware.RequiresBody(context))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBufferableJson(HttpRequest request, long maxBodyBytes)
        => request.ContentType is not null
           && request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
           && request.ContentLength is > 0
           && request.ContentLength <= maxBodyBytes;
}
