using SolTechnology.Core.HTTP;

// ReSharper disable once CheckNamespace
namespace System.Net.Http;

public static class HttpClientExtensions
{
    /// <summary>
    /// Begins constructing a request message for submission.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="path"></param>
    /// <returns><see cref="RequestBuilder"/> to use in constructing additional request details.</returns>
    public static RequestBuilder CreateRequest(this HttpClient httpClient, string path)
    {
        return new RequestBuilder(httpClient, path);
    }
}