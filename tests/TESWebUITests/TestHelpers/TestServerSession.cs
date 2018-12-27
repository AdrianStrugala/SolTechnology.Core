namespace DreamTravelITests.TestHelpers
{
    using DreamTravel;
    using DreamTravel.BestPath;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Controller = DreamTravel.BestPath.Controller;

    public sealed class TestServerSession : IDisposable
    {
        public TestServer Server { get; }


        public TestServerSession(Action<IServiceCollection> registerServices)
        {


            Startup._registerServices = registerServices;

            Server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
        }


        public async Task<HttpResponseMessage> PostCalculateBestPath(Query query)
        {
            HttpResponseMessage httpResponseMessage;

            using (var client = Server.CreateClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DreamAuthentication", "U29sVWJlckFsbGVz");

                httpResponseMessage = await client.PostAsync(Controller.Route,
                  new StringContent(JsonConvert.SerializeObject(query), Encoding.Default, "application/json"));
            }

            return httpResponseMessage;
        }

        public void Dispose()
        {
            Server.Dispose();

        }
    }
}

