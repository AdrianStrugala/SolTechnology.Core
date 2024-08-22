using SolTechnology.Core.Faker.FakesBase;
using WireMock.Logging;
using WireMock.Net.StandAlone;
using WireMock.Server;
using WireMock.Settings;

namespace SolTechnology.Core.Faker
{
    public class WireMockStartup
    {
        public WireMockServer WireMockServer = null!;
        private readonly List<IFakeApi> _fakeServices = new();

        public void Run(int port = 0, bool runAsServer = false)
        {
            var settings = new WireMockServerSettings
            {
                AllowPartialMapping = true,
                StartAdminInterface = true,
                ReadStaticMappings = false,
                Logger = new WireMockConsoleLogger(),
                Port = port
            };

            WireMockServer = StartServer(runAsServer, settings);
        }

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
            var fakeApi = _fakeServices.First(f =>
                f.GetType().BaseType != null &&
                f.GetType().BaseType!.GenericTypeArguments.Contains(typeof(TApiClient)));

            return (IFakeApiBuilderWithRequest<TApiClient>)fakeApi;
        }

        public void RegisterFakeApi(IFakeApi fakeApi)
        {
            fakeApi.Register(WireMockServer);
            _fakeServices.Add(fakeApi);
        }
    }
}
