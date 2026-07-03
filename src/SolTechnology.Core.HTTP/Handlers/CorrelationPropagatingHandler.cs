using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.HTTP.Handlers;

/// <summary>
/// <see cref="DelegatingHandler"/> that propagates the ambient correlation
/// identifier onto every outbound HTTP request. Registered automatically by
/// <c>AddSolHTTPClient&lt;,&gt;</c> and runs before the resilience handler so the
/// same headers are attached to every retry attempt.
///
/// <para>
/// Adds two headers (both via <see cref="System.Net.Http.Headers.HttpHeaders.TryAddWithoutValidation(string, string?)"/>
/// — caller-supplied values via <c>RequestBuilder.WithHeader</c> always win):
/// </para>
/// <list type="bullet">
///   <item>
///     <c>X-Correlation-Id</c> — friendly opaque value sourced from
///     <see cref="ICorrelationIdService.GetOrGenerate"/>. Set unconditionally
///     so non-OpenTelemetry downstreams and support workflows always have
///     something to quote.
///   </item>
///   <item>
///     <c>traceparent</c> — full W3C Trace Context value (RFC 9110) built via
///     <see cref="CorrelationId.TryBuildTraceParentFromCurrentActivity"/>.
///     Skipped when no <see cref="System.Diagnostics.Activity"/> is in scope,
///     since fabricating one would lie about a span id that has no
///     relationship to anything OpenTelemetry-aware downstreams could
///     correlate to.
///   </item>
/// </list>
/// </summary>
internal sealed class CorrelationPropagatingHandler(ICorrelationIdService correlationIdService) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Respect a header the caller (or an upstream OpenTelemetry / firm
        // correlation handler) has already attached. Doing this BEFORE we call
        // GetOrGenerate keeps us from seeding the AsyncLocal store in apps that
        // own their own correlation pipeline — otherwise we would silently
        // mint a second id and Core.Logging downstream would log a different
        // value than the one the partner sees on the wire.
        if (request.Headers.Contains(CorrelationId.HeaderKey))
        {
            return base.SendAsync(request, cancellationToken);
        }

        // GetOrGenerate seeds an AsyncLocal so that subsequent handlers, the
        // resilience pipeline, and any code observing ICorrelationIdService
        // within the same call observe the same id. For an ASP.NET Core inbound
        // request the LoggingMiddleware has already set this value; for a
        // background worker / first-call-from-a-job this is where it is born.
        var correlationId = correlationIdService.GetOrGenerate();

        request.Headers.TryAddWithoutValidation(CorrelationId.HeaderKey, correlationId.Value);

        // Delegated to Core.Logging so the inbound (response-header) and
        // outbound (request-header) traceparent formats can never drift apart.
        var traceParent = CorrelationId.TryBuildTraceParentFromCurrentActivity();
        if (traceParent is not null)
        {
            request.Headers.TryAddWithoutValidation(CorrelationId.TraceParentHeaderKey, traceParent);
        }

        return base.SendAsync(request, cancellationToken);
    }
}



