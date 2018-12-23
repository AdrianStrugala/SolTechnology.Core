namespace DreamTravelITests.TestHelpers
{
    using DreamTravel;
    using DreamTravel.BestPath;
    using DreamTravel.BestPath.Interfaces;
    using DreamTravel.SharedModels;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Newtonsoft.Json;
    using NSubstitute;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Controller = DreamTravel.BestPath.Controller;

    public sealed class TestServerSession : IDisposable
    {
        public TestServer Server { get; }


        public TestServerSession(Action<IServiceCollection> RegisterServices)
        {


            Startup.RegisterServices = RegisterServices;

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

