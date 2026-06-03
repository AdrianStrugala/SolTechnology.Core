using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.API.Testing
{
    /// <summary>
    /// Lightweight wrapper around <see cref="WebApplicationFactory{TEntryPoint}"/> that boots an
    /// in-memory ASP.NET Core host and exposes its <see cref="TestServer"/> + a ready-to-use
    /// <see cref="HttpClient"/>. Use from integration / component tests; never reference at
    /// runtime in production code.
    /// </summary>
    /// <typeparam name="TEntryPoint">A type in the entry point assembly of the application
    /// under test. Typically the <c>Program</c> class.</typeparam>
    public class APIFixture<TEntryPoint> : IDisposable where TEntryPoint : class
    {
        public TestServer TestServer { get; }

        public HttpClient ServerClient { get; }


        public APIFixture(IConfiguration? configuration = null, Action<IServiceCollection>? configureServices = null)
        {
            var webAppFactory = new WebApplicationFactory<TEntryPoint>()
                .WithWebHostBuilder(builder =>
                    {
                        if (configuration != null)
                        {
                            builder.UseConfiguration(configuration);
                        }

                        builder
                            .ConfigureServices((context, services) =>
                            {
                                services.AddLogging(l => l.AddConsole());
                                configureServices?.Invoke(services);
                            });
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


