using SolTechnology.Core.HTTP.Testing.Faker;
  using WireMock.Logging;
using WireMock.Net.StandAlone;
using WireMock.Server;
using WireMock.Settings;

namespace SolTechnology.Core.HTTP.Testing
{
    public class WireMockStartup
    {
        public WireMockServer WireMockServer = null!;
        private readonly List<IFakeApi> _fakeServices = new();

        /// <summary>Base URL of the running server (valid after <see cref="Run(int,bool)"/>), e.g. <c>http://localhost:54213</c>.</summary>
        public string Url => WireMockServer.Url!;

        /// <summary>The actual bound port — dynamic when <c>0</c> was requested.</summary>
        public int Port => WireMockServer.Ports[0];

        public void Run(int port = 0, bool runAsServer = false) =>
            Run(DefaultSettings(port), runAsServer);

        public void Run(WireMockServerSettings settings, bool runAsServer = false)
        {
            ArgumentNullException.ThrowIfNull(settings);
            WireMockServer = StartServer(runAsServer, settings);
        }

        /// <summary>
        /// Defaults tuned for large/parallel suites: dynamic port, admin interface <b>off</b>, no console
        /// logging. Pass your own <see cref="WireMockServerSettings"/> to <see cref="Run(WireMockServerSettings,bool)"/>
        /// to override (e.g. turn the admin interface back on for debugging).
        /// </summary>
        public static WireMockServerSettings DefaultSettings(int port = 0) => new()
        {
            AllowPartialMapping = true,
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

        public void RegisterFakeApi(IFakeApi fakeApi)
        {
            ArgumentNullException.ThrowIfNull(fakeApi);
            fakeApi.Register(WireMockServer);
            _fakeServices.Add(fakeApi);
        }
    }
}
