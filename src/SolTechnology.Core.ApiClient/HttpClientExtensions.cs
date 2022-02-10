using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace System.Net.Http
{
    public static class HttpClientExtensions
    {
        public static async Task<TResponse> GetAsync<TResponse>(this HttpClient httpClient, string url) where TResponse : class
        {
            return await SendJsonAsync<string, TResponse>(httpClient, url, HttpMethod.Get);
        }

        private static async Task<TResponse> SendJsonAsync<TRequest, TResponse>(this HttpClient httpClient, string url, HttpMethod httpMethod, TRequest content = null) where TRequest : class
        {
            HttpRequestMessage httpRequest = new HttpRequestMessage(httpMethod, url);

            if (content != null)
            {
                var contentJson = JsonConvert.SerializeObject(content);

                HttpContent httpContent = new StringContent(contentJson);
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.Add("Content-Type", "application/json");

                httpRequest.Content = httpContent;
            }

            var response = await httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode == false)
            {
                HandleErrors();
            }

            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }

        private static void HandleErrors()
        {
            throw new NotImplementedException();
        }
    }
}
