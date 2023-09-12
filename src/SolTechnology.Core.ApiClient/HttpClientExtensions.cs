using System.Net.Http.Headers;
using Newtonsoft.Json;
using SolTechnology.Avro;
using SolTechnology.Core.ApiClient;

// ReSharper disable once CheckNamespace
namespace System.Net.Http
{
    public static class HttpClientExtensions
    {
        public static async Task<TResponse> GetAsync<TResponse>(this HttpClient httpClient, string url, DataType dataType = DataType.Json) where TResponse : class
        {
            return await SendAsync<string, TResponse>(httpClient, url, HttpMethod.Get);
        }
        public static async Task<TResponse> PostAsync<TContent, TResponse>(this HttpClient httpClient, string url, TContent request, Dictionary<string, string> headers = null, DataType dataType = DataType.Json) where TContent : class
        {
            return await SendAsync<TContent, TResponse>(httpClient, url, HttpMethod.Post, request, headers);
        }

        public static async Task<TResponse> DeleteAsync<TContent, TResponse>(this HttpClient httpClient, string url, TContent request, DataType dataType = DataType.Json) where TContent : class
        {
            return await SendAsync<TContent, TResponse>(httpClient, url, HttpMethod.Delete, request);
        }

        public static async Task<TResponse> PutAsync<TContent, TResponse>(this HttpClient httpClient, string url, TContent request, DataType dataType = DataType.Json) where TContent : class
        {
            return await SendAsync<TContent, TResponse>(httpClient, url, HttpMethod.Put, request);
        }


        private static async Task<TResponse> SendAsync<TContent, TResponse>(
            this HttpClient httpClient,
            string url,
            HttpMethod httpMethod,
            TContent content = null,
            Dictionary<string, string> headers = null,
            DataType dataType = DataType.Json) 
            where TContent : class
        {
            switch (dataType)
            {
                case DataType.Json:
                    return await SendJsonAsync<TContent, TResponse>(httpClient, url, httpMethod, content, headers);
                case DataType.Avro:
                    return await SendAvroAsync<TContent, TResponse>(httpClient, url, httpMethod, content);
            }

            throw new ArgumentException($"Unknown data type: [{dataType}]");
        }


        private static async Task<TResponse> SendJsonAsync<TContent, TResponse>(
            this HttpClient httpClient, 
            string url,
            HttpMethod httpMethod, 
            TContent content = null,
            Dictionary<string, string> headers = null) 
            where TContent : class
        {
            HttpRequestMessage httpRequest = new HttpRequestMessage(httpMethod, url);

            if (content != null)
            {
                var contentJson = JsonConvert.SerializeObject(content);

                HttpContent httpContent = new StringContent(contentJson);
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                httpRequest.Content = httpContent;
            }

            if (headers != null)
            {
                foreach (var (key, value) in headers)
                {
                    httpRequest.Headers.Add(key, value);
                }
            }

            var response = await httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode == false)
            {
                HandleErrors(response);
            }

            var responseContent = await response.Content.ReadAsStringAsync();


            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }


        private static async Task<TResponse> SendAvroAsync<TContent, TResponse>(this HttpClient httpClient, string url, HttpMethod httpMethod, TContent content = null) where TContent : class
        {
            HttpRequestMessage httpRequest = new HttpRequestMessage(httpMethod, url);

            if (content != null)
            {
                var avroContent = AvroConvert.Serialize(content);

                HttpContent httpContent = new ByteArrayContent(avroContent);
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/avro");

                httpRequest.Content = httpContent;
            }

            var response = await httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode == false)
            {
                HandleErrors(response);
            }

            var responseContent = await response.Content.ReadAsByteArrayAsync();


            return AvroConvert.Deserialize<TResponse>(responseContent);
        }

        private static void HandleErrors(HttpResponseMessage httpResponseMessage)
        {
            throw new Exception(httpResponseMessage.ReasonPhrase);
        }
    }
}
