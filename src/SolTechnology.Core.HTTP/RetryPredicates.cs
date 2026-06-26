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
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return true; // no body — trust the status code

            using var doc = JsonDocument.Parse(body);

            // Standard ProblemDetails extensions shape: { "recoverable": true/false }
            if (doc.RootElement.TryGetProperty("recoverable", out var prop) &&
                prop.ValueKind == JsonValueKind.False)
            {
                return false; // explicitly non-recoverable — do not retry
            }

            return true; // recoverable or not present — allow retry
        }
        catch
        {
            // Parse failure — benefit of the doubt, let the retry proceed.
            return true;
        }
    };
}

