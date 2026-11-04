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
        // Checked before GetOrGenerate, so hosts that own correlation never get a second id in the AsyncLocal store.
        if (request.Headers.Contains(CorrelationId.HeaderKey))
        {
            return base.SendAsync(request, cancellationToken);
        }

        // Seeds the AsyncLocal, so retries and later handlers in this call see the same id.
        var correlationId = correlationIdService.GetOrGenerate();

        request.Headers.TryAddWithoutValidation(CorrelationId.HeaderKey, correlationId.Value);

        // Built by Core.Logging, so inbound and outbound traceparent formats cannot drift apart.
        var traceParent = CorrelationId.TryBuildTraceParentFromCurrentActivity();
        if (traceParent is not null)
        {
            request.Headers.TryAddWithoutValidation(CorrelationId.TraceParentHeaderKey, traceParent);
        }

        return base.SendAsync(request, cancellationToken);
    }
}



