namespace SolTechnology.Core.ApiClient.Connection;

public interface IApiClientFactory
{
    System.Net.Http.HttpClient GetClient(string clientName);
}