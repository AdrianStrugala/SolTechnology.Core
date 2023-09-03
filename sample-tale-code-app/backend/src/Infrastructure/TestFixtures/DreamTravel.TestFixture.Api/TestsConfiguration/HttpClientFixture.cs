using System.Net.Http.Headers;
using DreamTravel.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;

namespace DreamTravel.TestFixture.Api.TestsConfiguration
{
    public class HttpClientFixture : IDisposable
    {
        public TestServer TestServer { get; }

        public HttpClient ServerClient { get; }

        // public FluentMockServer FakeServer { get; }

        public HttpClientFixture()
        {
            // FakeServer = WireMockService.RunServer();

            TestServer = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());

            ServerClient = TestServer.CreateClient();
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string uri, Dictionary<string, string> headers = null)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            ApplyHeaders(httpRequestMessage, headers);

            var httpResponseMessage = await ServerClient.SendAsync(httpRequestMessage);

            var response = new ApiResponse<T>();
            response.RawBody = await httpResponseMessage.Content.ReadAsStringAsync();
            response.HttpStatusCode = httpResponseMessage.StatusCode;
            response.ReasonPhrase = httpResponseMessage.ReasonPhrase;
            response.Headers = httpResponseMessage.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());

            return response;
        }

        private void ApplyHeaders(HttpRequestMessage httpRequestMessage, Dictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    httpRequestMessage.Headers.Add(header.Key, header.Value);
                }
            }
            httpRequestMessage.Headers.Add("Authorization", "DreamAuthentication U29sVWJlckFsbGVz");
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string uri, object data, Dictionary<string, string> headers = null)
        {
            var myContent = JsonConvert.SerializeObject(data);
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            httpRequestMessage.Content = byteContent;

            ApplyHeaders(httpRequestMessage, headers);

            var httpResponseMessage = await ServerClient.SendAsync(httpRequestMessage);

            var response = new ApiResponse<T>();
            response.RawBody = await httpResponseMessage.Content.ReadAsStringAsync();
            response.HttpStatusCode = httpResponseMessage.StatusCode;
            response.ReasonPhrase = httpResponseMessage.ReasonPhrase;
            response.Headers = httpResponseMessage.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());

            return response;
        }


        public void Dispose()
        {
            ServerClient?.Dispose();
            TestServer?.Dispose();
            // FakeServer?.Dispose();
        }
    }
}