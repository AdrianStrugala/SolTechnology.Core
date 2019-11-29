using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DreamTravel.Infrastructure.Http
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PostAsJson(this HttpClient httpClient, string requestUri, object content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json")
            };

            return await httpClient.SendAsync(request);
        }
    }
}
