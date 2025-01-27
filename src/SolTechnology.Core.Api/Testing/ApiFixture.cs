using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Api.Testing
{
    /// <typeparam name="TEntryPoint">A type in the entry point assembly of the application.
    /// Typically the Startup or Program classes can be used.</typeparam>
    public class ApiFixture<TEntryPoint> : IDisposable where TEntryPoint : class
    {
        public TestServer TestServer { get; }

        public HttpClient ServerClient { get; }


        public ApiFixture(IConfiguration? configuration = null)
        {
            var webAppFactory = new WebApplicationFactory<TEntryPoint>()
                .WithWebHostBuilder(builder =>
                    {
                        if(configuration != null)
                        {
                            builder
                                .UseConfiguration(configuration);
                        }

                        builder
                            .ConfigureServices((context, services) =>
                                services.AddLogging(l => l.AddConsole()));
                    });

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
