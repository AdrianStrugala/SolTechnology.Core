namespace SolTechnology.Core.HTTP.Tests;

interface ISampleHTTPClient
{
    string DownloadSth();

    System.Net.Http.HttpClient HttpClient { get; set; }
}

class SampleHTTPClient : ISampleHTTPClient
{
    public System.Net.Http.HttpClient HttpClient { get; set; }

    public SampleHTTPClient(System.Net.Http.HttpClient httpClient)
    {
        HttpClient = httpClient;
    }
    public string DownloadSth()
    {
        return "it";
    }

}