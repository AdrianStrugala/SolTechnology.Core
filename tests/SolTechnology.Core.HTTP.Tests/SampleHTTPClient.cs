namespace SolTechnology.Core.HTTP.Tests;

internal interface ISampleHTTPClient
{
    System.Net.Http.HttpClient HttpClient { get; }
}

internal sealed class SampleHTTPClient : ISampleHTTPClient
{
    public System.Net.Http.HttpClient HttpClient { get; }

    public SampleHTTPClient(System.Net.Http.HttpClient httpClient) => HttpClient = httpClient;
}
