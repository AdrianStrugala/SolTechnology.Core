using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Api.Testing
{
    /// <typeparam name="TEntryPoint">A type in the entry point assembly of the application.
    /// Typically the Startup or Program classes can be used.</typeparam>
    public class ApiFixture<TEntryPoint> : IDisposable where TEntryPoint : class
    {
        public TestServer TestServer { get; }

        public HttpClient ServerClient { get; }


        public ApiFixture(Action<IServiceCollection> configureServices = null)
        {
            var webAppFactory = new WebApplicationFactory<TEntryPoint>()
                .WithWebHostBuilder(builder =>
                    builder
                        .ConfigureAppConfiguration((_, config) => config.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.functional.tests.json")))
                        .ConfigureServices(configureServices ?? (_ => { })));

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
