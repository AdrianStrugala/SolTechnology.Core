namespace SolTechnology.Core.ApiClient.Tests;

interface ISampleApiClient
{
    string DownloadSth();

    System.Net.Http.HttpClient HttpClient { get; set; }
}

class SampleApiClient : ISampleApiClient
{
    public System.Net.Http.HttpClient HttpClient { get; set; }

    public SampleApiClient(System.Net.Http.HttpClient httpClient)
    {
        HttpClient = httpClient;
    }
    public string DownloadSth()
    {
        return "it";
    }

}