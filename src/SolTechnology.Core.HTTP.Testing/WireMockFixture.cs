using SolTechnology.Core.HTTP.Testing.Faker;
 using WireMock.Logging;

namespace SolTechnology.Core.HTTP.Testing
{
    public class WireMockFixture : IDisposable, IAsyncDisposable
    {
        private WireMockStartup? _wireMockStartup;

        private WireMockStartup Startup => _wireMockStartup
            ?? throw new InvalidOperationException("Call Initialize() before using the WireMockFixture.");

        /// <summary>Base URL of the running fake server (valid after <see cref="Initialize"/>), e.g. <c>http://localhost:54213</c>.</summary>
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
        public WireMockStartup Initialize(int port = 0)
        {
            _wireMockStartup = new WireMockStartup();
            _wireMockStartup.Run(port);

            return _wireMockStartup;
        }

        public void RegisterFakeApi(IFakeApi fakeApi) =>
            Startup.RegisterFakeApi(fakeApi);

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

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
