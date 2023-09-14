using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace TaleCode.FunctionalTests.TestsConfiguration
{
    public class BackgroundWorkerFixture : IDisposable
    {
        public TestServer TestServer { get; }

        public HttpClient ServerClient { get; }


        public BackgroundWorkerFixture()
        {
            var webAppFactory = new WebApplicationFactory<SolTechnology.TaleCode.BackgroundWorker.Program>()
                .WithWebHostBuilder(builder =>
                    builder
                        .ConfigureAppConfiguration((_, config) => config.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.functional.tests.json")))
                        .ConfigureServices(_ =>
                        {
                            // set up custom services
                        }));

            TestServer = webAppFactory.Server;
            TestServer.PreserveExecutionContext = true;

            ServerClient = TestServer.CreateClient();
        }

        public void Dispose()
        {
            ServerClient?.Dispose();
            TestServer?.Dispose();
        }
    }
}
