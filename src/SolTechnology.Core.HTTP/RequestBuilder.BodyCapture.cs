using System.Buffers;

namespace SolTechnology.Core.HTTP;

public partial class RequestBuilder
{
    /// <summary>
    /// Upper bound on response-body bytes captured in <see cref="HttpRequestFailedException.ResponseBody"/>.
    /// 8 KB is enough for any structured-error JSON payload (RFC 7807 problem details, GraphQL
    /// errors, vendor-specific shapes) while keeping a hostile or accidental 50 MB HTML page
    /// from pinning memory inside the exception object as it flows through logging sinks.
    /// </summary>
    internal const int MaxCapturedBodyBytes = 8 * 1024;

    private const string TruncationMarker = "… [response body truncated]";

    /// <summary>
    /// Buffers <paramref name="content"/> when it fits in <see cref="MaxCapturedBodyBytes"/>, so it
    /// can be read more than once. Returns <c>false</c> for a larger body or a failed read.
    /// </summary>
    internal static async Task<bool> TryBufferCappedBody(HttpContent content, CancellationToken cancellationToken)
    {
        try
        {
            await content.LoadIntoBufferAsync(MaxCapturedBodyBytes, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    /// <summary>
    /// Builds and throws a <see cref="HttpRequestFailedException"/> carrying the request
    /// method, URI, status, reason phrase, and (best-effort) response body. The body is
    /// captured only when the policy passed to this request's <c>CreateRequest</c> overload sets
    /// <see cref="HttpPolicyConfiguration.IncludeResponseBodyInException"/> — by default it is off
    /// so upstream payloads (which may contain PII or secrets) never reach the exception object
    /// and, transitively, the logging pipeline.
    /// </summary>
    private async Task ThrowOnFailure(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var policy = ResolvePolicy(request);
        var body = policy.IncludeResponseBodyInException
            ? await TryReadCappedBody(response, cancellationToken).ConfigureAwait(false)
            : null;

        var uri = request.RequestUri ?? _httpClient.BaseAddress;

        throw new HttpRequestFailedException(
            request.Method,
            uri,
            response.StatusCode,
            response.ReasonPhrase,
            body);
    }

    /// <summary>
    /// Resolves the effective <see cref="HttpPolicyConfiguration"/> for a request.
    /// Order: per-request options (set by <see cref="BuildRequest"/>) → builder-scoped
    /// <see cref="_policy"/> → defaults. A null policy at every layer falls back to
    /// the conservative production defaults defined on the type itself.
    /// </summary>
    private HttpPolicyConfiguration ResolvePolicy(HttpRequestMessage request)
    {
        if (request.Options.TryGetValue(PolicyOptionsKey, out var fromOptions) && fromOptions is not null)
        {
            return fromOptions;
        }

        return _policy ?? new HttpPolicyConfiguration();
    }

    /// <summary>
    /// Reads up to <see cref="MaxCapturedBodyBytes"/> of the response body for inclusion in
    /// <see cref="HttpRequestFailedException.ResponseBody"/>, with a pooled staging buffer, so a
    /// huge error page cannot stall the caller or pin large allocations. Returns <c>null</c> when
    /// the body is empty or cannot be read; rethrows only the caller's cancellation.
    /// </summary>
    private static async Task<string?> TryReadCappedBody(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // One byte over the cap reveals truncation, so the text can say so.
        const int capacity = MaxCapturedBodyBytes + 1;
        var buffer = ArrayPool<byte>.Shared.Rent(capacity);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var read = 0;
            while (read < capacity)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read, capacity - read), cancellationToken).ConfigureAwait(false);
                if (n == 0)
                {
                    break;
                }
                read += n;
            }

            if (read == 0)
            {
                return null;
            }

            var truncated = read > MaxCapturedBodyBytes;
            var usable = truncated ? MaxCapturedBodyBytes : read;
            var text = System.Text.Encoding.UTF8.GetString(buffer, 0, usable);

            return truncated ? text + TruncationMarker : text;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Best effort: status and URI still describe the failure without the body.
            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
