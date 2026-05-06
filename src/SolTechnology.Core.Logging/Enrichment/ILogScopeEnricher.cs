using Microsoft.AspNetCore.Http;

namespace SolTechnology.Core.Logging.Enrichment;

/// <summary>
/// Contributes properties to the per-request logger scope. Implementations are resolved
/// from DI by the request-logging middleware and invoked once per request, after the
/// correlation id is established but before the inner pipeline runs.
/// </summary>
/// <remarks>
/// <para>Property naming convention: <c>PascalCase</c> (matches MEL / Serilog / App Insights).</para>
/// <para>Implementations must not throw — exceptions are caught by the middleware and logged
/// at <see cref="Microsoft.Extensions.Logging.LogLevel.Warning"/> so a faulty enricher cannot
/// take down the request.</para>
/// </remarks>
public interface ILogScopeEnricher
{
    /// <summary>
    /// Adds zero or more properties to <paramref name="scope"/>. The middleware passes a single
    /// shared dictionary that is then handed to <c>ILogger.BeginScope</c> after every enricher
    /// has run, so all contributors share one scope entry (cheaper than nested scopes).
    /// </summary>
    void Enrich(HttpContext context, IDictionary<string, object?> scope);
}

