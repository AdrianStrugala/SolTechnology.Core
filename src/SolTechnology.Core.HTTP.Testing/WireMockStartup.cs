using SolTechnology.Core.HTTP.Testing.Faker;
  using WireMock.Logging;
using WireMock.Net.StandAlone;
using WireMock.Server;
using WireMock.Settings;

namespace SolTechnology.Core.HTTP.Testing
{
    /// <summary>
    /// Starts a WireMock.Net server and keeps the fake APIs registered on it. <see cref="WireMockFixture"/> wraps it
    /// for tests; use it directly when you need your own <see cref="WireMockServerSettings"/>.
    /// </summary>
    public class WireMockStartup
    {
        /// <summary>The running WireMock server (valid after <see cref="Run(int,bool)"/>); register stubs on it directly.</summary>
        public WireMockServer WireMockServer { get; private set; } = null!;

        private readonly List<IFakeApi> _fakeServices = new();

        /// <summary>Base URL of the running server (valid after <see cref="Run(int,bool)"/>), e.g. <c>http://localhost:54213</c>.</summary>
        public string Url => WireMockServer.Url!;

        /// <summary>The actual bound port — dynamic when <c>0</c> was requested.</summary>
        public int Port => WireMockServer.Ports[0];

        /// <summary>Starts the server with <see cref="DefaultSettings"/> on <paramref name="port"/> (<c>0</c> picks a free port).</summary>
        /// <param name="port">The port to listen on; <c>0</c> picks a free one.</param>
        /// <param name="runAsServer">Starts WireMock's standalone server, which also logs its ports to the console.</param>
        public void Run(int port = 0, bool runAsServer = false) =>
            Run(DefaultSettings(port), runAsServer);

        /// <summary>Starts the server with your own <paramref name="settings"/>, for example to allow partial matching again.</summary>
        /// <param name="settings">The WireMock settings; start from <see cref="DefaultSettings"/> to keep its defaults.</param>
        /// <param name="runAsServer">Starts WireMock's standalone server, which also logs its ports to the console.</param>
        public void Run(WireMockServerSettings settings, bool runAsServer = false)
        {
            ArgumentNullException.ThrowIfNull(settings);
            WireMockServer = StartServer(runAsServer, settings);
        }

        /// <summary>
        /// Defaults tuned for large/parallel suites: dynamic port, strict matching (a request must match every
        /// matcher of a mapping, otherwise the server answers 404), admin interface <b>off</b>, no console
        /// logging. Pass your own <see cref="WireMockServerSettings"/> to <see cref="Run(WireMockServerSettings,bool)"/>
        /// to override (e.g. <c>AllowPartialMapping = true</c>, or the admin interface back on for debugging).
        /// </summary>
        public static WireMockServerSettings DefaultSettings(int port = 0) => new()
        {
            AllowPartialMapping = false,
            StartAdminInterface = false,
            ReadStaticMappings = false,
            Logger = new WireMockNullLogger(),
            Port = port
        };

        private static WireMockServer StartServer(bool runAsServer, WireMockServerSettings settings)
        {
            if (!runAsServer)
            {
                return WireMockServer.Start(settings);
            }

            var standaloneServer = StandAloneApp.Start(settings);
            Console.WriteLine(@"WireMockServer running at ports: [{0}]", string.Join(",", standaloneServer.Ports));
            return standaloneServer;
        }

        /// <summary>Returns the arrange builder of the one registered fake that implements <typeparamref name="TApiClient"/>.</summary>
        /// <exception cref="InvalidOperationException">No fake, or more than one, implements <typeparamref name="TApiClient"/>.</exception>
        public IFakeApiBuilderWithRequest<TApiClient> GetFakeApi<TApiClient>() where TApiClient : class
        {
            // The fake implements the client interface directly, so match on TApiClient (no BaseType
            // reflection) and wrap it in the strongly-typed builder.
            var matches = _fakeServices.OfType<TApiClient>().ToList();

            return matches.Count switch
            {
                1 => new FakeApiBuilder<TApiClient>((FakeApiBase)(object)matches[0]),
                0 => throw new InvalidOperationException(
                    $"No fake API registered for '{typeof(TApiClient).Name}'. Register one with " +
                    $"RegisterFakeApi(new YourFake()) where YourFake : FakeApiBase, {typeof(TApiClient).Name} " +
                    $"before calling Fake<{typeof(TApiClient).Name}>(). " +
                    $"Currently registered: [{string.Join(", ", _fakeServices.Select(f => f.GetType().Name))}]."),
                _ => throw new InvalidOperationException(
                    $"Multiple fake APIs registered for '{typeof(TApiClient).Name}' " +
                    $"([{string.Join(", ", matches.Select(m => m.GetType().Name))}]). Register exactly one per client interface.")
            };
        }

        /// <summary>Attaches <paramref name="fakeApi"/> to the running server so its stubs can be arranged.</summary>
        public void RegisterFakeApi(IFakeApi fakeApi)
        {
            ArgumentNullException.ThrowIfNull(fakeApi);
            fakeApi.Register(WireMockServer);
            _fakeServices.Add(fakeApi);
        }
    }
}
