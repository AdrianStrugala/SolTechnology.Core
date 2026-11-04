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
        /// through <see cref="HttpRequestMessage.Options"/> under
        /// <see cref="RequestBuilder.PolicyOptionsKey"/>.
        /// <para>
        /// Only <see cref="HttpPolicyConfiguration.IncludeResponseBodyInException"/> is honoured
        /// per request, when a throwing verb builds <see cref="HttpRequestFailedException"/>.
        /// Retry, circuit breaker and timeouts always follow the named client's policy registered
        /// by <c>AddSolHTTPClient</c>. To apply that client's configured flag, pass
        /// <c>IOptionsMonitor&lt;HttpPolicyConfiguration&gt;.Get(clientName)</c> here.
        /// </para>
        /// </summary>
        public RequestBuilder CreateRequest(string path, HttpPolicyConfiguration policy)
        {
            return new RequestBuilder(httpClient, path, policy);
        }
    }
}
