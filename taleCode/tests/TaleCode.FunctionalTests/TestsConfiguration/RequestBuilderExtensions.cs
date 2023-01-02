using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;

namespace TaleCode.FunctionalTests.TestsConfiguration
{
    public static class RequestBuilderExtensions
    {
        public static Task<ResponseWithData<T>> GetAsync<T>(this RequestBuilder builder) =>
            builder.SendAsync<T>(HttpMethod.Get);

        public static Task<ResponseWithData<T>> PostAsync<T>(this RequestBuilder builder) =>
            builder.SendAsync<T>(HttpMethod.Post);

        public static async Task<ResponseWithData<T>> SendAsync<T>(this RequestBuilder builder, HttpMethod httpMethod)
        {
            var response = await builder.SendAsync(httpMethod.Method);
            var data = await response.Content.ReadFromJsonAsync<T>();
            return new ResponseWithData<T>(response, data);
        }
    }
}
