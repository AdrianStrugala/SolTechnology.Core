using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace TaleCode.IntegrationTests.Api
{
    public class ApiFixture : IDisposable
    {
        public TestServer TestServer { get; }

        public HttpClient ServerClient { get; }


        public ApiFixture()
        {
            var webAppFactory = new WebApplicationFactory<Program>()
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
