using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using SolTechnology.TaleCode.BackgroundWorker;

namespace TaleCode.ComponentTests.TestsConfiguration
{
    public class BackgroundWorkerFixture : IDisposable
    {
        public TestServer TestServer { get; }

        public HttpClient ServerClient { get; }


        public BackgroundWorkerFixture()
        {
            var webAppFactory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                    builder
                        .ConfigureAppConfiguration((_, config) => config.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.component.tests.json")))
                        .ConfigureServices(_ =>
                        {
                            // set up custom services
                        }));

            TestServer = webAppFactory.Server;

            ServerClient = TestServer.CreateClient();
        }

        public void Dispose()
        {
            ServerClient?.Dispose();
            TestServer?.Dispose();
        }
    }
}
