using SolTechnology.Core.HTTP.Testing.Faker;
 using WireMock.Logging;
using WireMock.Settings;

namespace SolTechnology.Core.HTTP.Testing
{
    /// <summary>
    /// Test fixture around <see cref="WireMockStartup"/>: starts a fake HTTP server on a dynamic port, registers fake
    /// APIs and arranges their responses. Start it in <c>[OneTimeSetUp]</c>, call <see cref="Reset"/> between tests,
    /// and dispose it at the end. By default a request must match every matcher of a stub, otherwise it gets 404;
    /// <see cref="Initialize(WireMockServerSettings)"/> opts back into partial matching.
    /// </summary>
    public class WireMockFixture : IDisposable, IAsyncDisposable
    {
        private WireMockStartup? _wireMockStartup;

        private WireMockStartup Startup => _wireMockStartup
            ?? throw new InvalidOperationException("Call Initialize() before using the WireMockFixture.");

        /// <summary>Base URL of the running fake server (valid after <see cref="Initialize(int)"/>), e.g. <c>http://localhost:54213</c>.</summary>
        public string Url => Startup.Url;

        /// <summary>The actual bound port — dynamic when <c>0</c> was requested.</summary>
        public int Port => Startup.Port;

        /// <summary>Every request the server received — assert that a client actually called the fake.</summary>
        public IEnumerable<ILogEntry> LogEntries => Startup.WireMockServer.LogEntries;

        /// <summary>
        /// Starts the fake server. Defaults to a <b>dynamic port</b> (<c>0</c>) so parallel suites never
        /// collide on a fixed port — read <see cref="Url"/>/<see cref="Port"/> to wire your client (e.g.
        /// inject it into configuration). Pass a fixed port only when a consumer hard-codes the address.
        /// </summary>
        public WireMockStartup Initialize(int port = 0) =>
            Initialize(WireMockStartup.DefaultSettings(port));

        /// <summary>
        /// Starts the fake server with your own <paramref name="settings"/>; start from
        /// <see cref="WireMockStartup.DefaultSettings"/> and set <c>AllowPartialMapping = true</c> to serve the
        /// closest stub when no stub matches every matcher.
        /// </summary>
        public WireMockStartup Initialize(WireMockServerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            _wireMockStartup = new WireMockStartup();
            _wireMockStartup.Run(settings);

            return _wireMockStartup;
        }

        /// <summary>Attaches <paramref name="fakeApi"/> to the server so <see cref="Fake{T}"/> can arrange it.</summary>
        public void RegisterFakeApi(IFakeApi fakeApi) =>
            Startup.RegisterFakeApi(fakeApi);

        /// <summary>Starts arranging a stub on the registered fake that implements the client interface <typeparamref name="T"/>.</summary>
        public IFakeApiBuilderWithRequest<T> Fake<T>() where T : class =>
            Startup.GetFakeApi<T>();

        /// <summary>
        /// Clears all configured request/response mappings and recorded requests — the between-test reset.
        /// Call this from a per-test teardown to isolate tests; it does <b>not</b> stop the server.
        /// </summary>
        public void Reset() => Startup.WireMockServer.Reset();

        /// <summary>Stops the server and frees the port. Call once, at the end of the fixture's life.</summary>
        public void Dispose()
        {
            if (_wireMockStartup is { WireMockServer: { } server })
            {
                server.Stop();
                server.Dispose();
            }
        }

        /// <summary>Stops the server; same as <see cref="Dispose"/>.</summary>
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
