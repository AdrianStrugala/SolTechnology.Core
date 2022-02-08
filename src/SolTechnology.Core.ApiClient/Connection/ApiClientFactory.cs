using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.ApiClient.Connection
{
    public class ApiClientFactory : IApiClientFactory
    {
        private readonly ConcurrentDictionary<string, System.Net.Http.HttpClient> _httpClients = new ConcurrentDictionary<string, System.Net.Http.HttpClient>();

        public ApiClientFactory(IOptions<ApiClientConfiguration> apiClientConfiguration)
        {
            foreach (var httpClientConfiguration in apiClientConfiguration.Value.HttpClients)
            {
                var httpClient = new System.Net.Http.HttpClient();
                httpClient.BaseAddress = new Uri(httpClientConfiguration.BaseAddress);
                if (httpClientConfiguration.TimeoutSeconds.HasValue)
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(httpClientConfiguration.TimeoutSeconds.Value);
                }

                foreach (var header in httpClientConfiguration.Headers)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Name, header.Value);
                }

                _httpClients.TryAdd(httpClientConfiguration.Name.ToLowerInvariant(), httpClient);
            }
        }

        public System.Net.Http.HttpClient GetClient(string clientName)
        {
            _httpClients.TryGetValue(clientName.ToLowerInvariant(), out var httpClient);

            if (httpClient != null)
            {
                return httpClient;
            }

            throw new ArgumentException(
                $"Http Api Client named [{clientName}] was not found. Provide its configuration in services registration.");

        }
    }
}
