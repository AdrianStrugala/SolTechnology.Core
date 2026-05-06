using Microsoft.AspNetCore.Http;

namespace SolTechnology.Core.Logging.Enrichment;

/// <summary>
/// Hint to <c>LoggingMiddleware</c> that an enricher needs the JSON request body
/// to be parsed and stashed in <see cref="HttpContext.Items"/> before <c>Enrich</c> runs.
/// Enrichers that do not need the body must not implement this interface.
/// </summary>
internal interface IRequiresRequestBody
{
    bool RequiresBody(HttpContext context);
}

