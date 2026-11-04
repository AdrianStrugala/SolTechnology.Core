using System.Text.Json;

namespace SolTechnology.Core.HTTP;

/// <summary>
/// Pre-built retry predicates for use with <see cref="HttpPolicyConfiguration.RetryPredicate"/>.
/// </summary>
public static class RetryPredicates
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Retries only when the response body indicates all errors are recoverable.
    /// Parses the <c>extensions.recoverable</c> field from a standard ProblemDetails envelope
    /// (as emitted by <c>Core.Api</c>'s <c>ApiProblemDetailsFactory</c>).
    /// <para>
    /// Returns <c>true</c> (allow retry) when:
    /// <list type="bullet">
    ///   <item>The body contains <c>"recoverable": true</c>.</item>
    ///   <item>The body cannot be parsed (benefit of the doubt — let the standard status-code
    ///         retry logic handle it).</item>
    ///   <item>The body is larger than 8 KiB; it is not read past that cap.</item>
    /// </list>
    /// Returns <c>false</c> (stop retrying) when:
    /// <list type="bullet">
    ///   <item>The body contains <c>"recoverable": false</c> — a deterministic business rejection
    ///         that will never succeed on retry.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static Func<HttpResponseMessage, ValueTask<bool>> RecoverableOnly { get; } = async response =>
    {
        // The resilience pipeline has already buffered the body with the request's token; this is a no-op then.
        if (!await RequestBuilder.TryBufferCappedBody(response.Content, CancellationToken.None))
        {
            return true;
        }

        try
        {
            var body = await response.Content.ReadAsByteArrayAsync();
            if (body.Length == 0)
            {
                return true;
            }

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("recoverable", out var prop)
                || prop.ValueKind != JsonValueKind.False;
        }
        catch (JsonException)
        {
            // Not JSON: benefit of the doubt, the status code decides.
            return true;
        }
    };
}
