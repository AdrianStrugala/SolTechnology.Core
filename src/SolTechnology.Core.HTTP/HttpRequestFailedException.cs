using System.Net;

namespace SolTechnology.Core.HTTP;

/// <summary>
/// Thrown by <see cref="RequestBuilder"/> when an outbound HTTP request returns a non-success
/// status code. Carries the full request/response context for diagnostics while keeping
/// sensitive content out of the exception's <see cref="Exception.Message"/>.
/// </summary>
/// <remarks>
/// <para>
/// Inherits from <see cref="HttpRequestException"/> so existing
/// <c>catch (HttpRequestException)</c> handlers continue to work. <c>HttpRequestException.StatusCode</c>
/// (added in .NET 5) is populated.
/// </para>
/// <para>
/// <strong>Why response body is NOT in <see cref="Exception.Message"/>:</strong> the message
/// flows into every logging sink, APM, and crash dump verbatim — putting the raw upstream
/// response there is a reliable way to leak tokens, PII, and customer data. Callers that
/// actually need the body opt in by reading <see cref="ResponseBody"/>.
/// </para>
/// </remarks>
public sealed class HttpRequestFailedException : HttpRequestException
{
    /// <summary>HTTP verb of the failed request. May be <c>null</c> if the builder never assigned one.</summary>
    public HttpMethod? Method { get; }

    /// <summary>Absolute or relative request URI as known to the builder.</summary>
    public Uri? RequestUri { get; }

    /// <summary>Reason phrase from the response status line. Useful for human-readable logs.</summary>
    public string? ReasonPhrase { get; }

    /// <summary>
    /// Raw response body as a string. Deliberately separate from <see cref="Exception.Message"/>
    /// so callers opt into reading it. May be <c>null</c> if the body could not be read
    /// (stream already consumed, network error during read, etc.).
    /// </summary>
    public string? ResponseBody { get; }

    public HttpRequestFailedException(
        HttpMethod? method,
        Uri? requestUri,
        HttpStatusCode statusCode,
        string? reasonPhrase,
        string? responseBody)
        : base(
            // Metadata-only message. ResponseBody must NOT be inlined here.
            $"HTTP {method?.Method ?? "?"} {requestUri?.ToString() ?? "(no uri)"} failed with status {(int)statusCode} {reasonPhrase}.",
            inner: null,
            statusCode: statusCode)
    {
        Method = method;
        RequestUri = requestUri;
        ReasonPhrase = reasonPhrase;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Returns the exception's metadata-only representation. <see cref="ResponseBody"/>
    /// is deliberately <em>not</em> included so the default formatter used by Serilog,
    /// Application Insights, Sentry and every other sink that calls <c>ToString()</c>
    /// on a thrown exception cannot ship upstream payloads to log storage. Callers
    /// that explicitly need the body must read <see cref="ResponseBody"/> directly
    /// (and ideally redact before logging).
    /// </summary>
    public override string ToString()
    {
        // Mirrors the base format but omits the InnerException dump (always null
        // here) and any property-reflection that would expose ResponseBody.
        return $"{GetType().FullName}: {Message}{Environment.NewLine}{StackTrace}";
    }
}

