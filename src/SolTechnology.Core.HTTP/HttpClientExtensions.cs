namespace SolTechnology.Core.HTTP;

/// <summary>
/// Entry point of the <see cref="RequestBuilder"/> fluent API on <see cref="HttpClient"/>.
/// </summary>
public static class HttpClientExtensions
{
    /// <param name="httpClient">Client that sends the request.</param>
    extension(HttpClient httpClient)
    {
        /// <summary>
        /// Begins constructing a request message for submission.
        /// </summary>
        /// <returns><see cref="RequestBuilder"/> to chain further request details.</returns>
        public RequestBuilder CreateRequest(string path)
        {
            return new RequestBuilder(httpClient, path);
        }

        /// <summary>
        /// Begins constructing a request message that carries <paramref name="policy"/>
        /// through <see cref="HttpRequestMessage.Options"/>. Downstream code
        /// (resilience pipeline, error formatter) reads the policy via
        /// <see cref="RequestBuilder.PolicyOptionsKey"/> instead of pulling it from DI.
        /// <para>
        /// Use this overload when a single request needs to override the client-wide
        /// policy (e.g. <c>IncludeResponseBodyInException = true</c> for a one-off
        /// debug call) — typed-client implementations should pull
        /// <c>IOptionsMonitor&lt;HttpPolicyConfiguration&gt;.Get(clientName)</c> and
        /// pass it here.
        /// </para>
        /// </summary>
        public RequestBuilder CreateRequest(string path, HttpPolicyConfiguration policy)
        {
            return new RequestBuilder(httpClient, path, policy);
        }
    }
}
