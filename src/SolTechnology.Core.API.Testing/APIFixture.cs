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
        private readonly WebApplicationFactory<TEntryPoint> _factory = new();
        private bool _disposed;

        /// <summary>The in-memory server that hosts the application under test.</summary>
        public TestServer TestServer { get; }

        /// <summary>A client whose requests go straight to <see cref="TestServer"/>.</summary>
        public HttpClient ServerClient { get; }


        /// <summary>Boots the application of <typeparamref name="TEntryPoint"/> in memory.</summary>
        /// <param name="configuration">Configuration whose values override the application's settings, e.g. from <see cref="TestConfigurationBuilder"/>.</param>
        /// <param name="configureServices">Runs after the application's service registrations, e.g. to replace a dependency.</param>
        public APIFixture(IConfiguration? configuration = null, Action<IServiceCollection>? configureServices = null)
        {
            var webAppFactory = _factory
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

        /// <summary>
        /// Stops the in-memory host (running every <c>IHostedService.StopAsync</c>) and disposes the
        /// client, the <see cref="TestServer"/> and the underlying <see cref="WebApplicationFactory{TEntryPoint}"/>.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ServerClient.Dispose();
            // Disposing the root factory also disposes the factory derived by WithWebHostBuilder and its host.
            _factory.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}


