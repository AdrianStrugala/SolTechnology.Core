using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
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
            var webAppFactory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                    builder
                        .ConfigureAppConfiguration((_, config) => config.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.functional.tests.json")))
                        .ConfigureServices(_ =>
                        {
                            // set up custom services
                        }));

            // var webHostBuilder = new WebHostBuilder()
            //     .UseStartup<Startup>()
            //     .ConfigureAppConfiguration((_, config) => config.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")))
            //     .ConfigureAppConfiguration((_, config) => config.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.functional.tests.json")))
            //     .ConfigureServices(_ =>
            //     {
            //         // set up custom services
            //     });
            //
            // TestServer = new TestServer(webHostBuilder)
            // {
            //     PreserveExecutionContext = true
            // };
            // ServerClient = TestServer.CreateClient();

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
