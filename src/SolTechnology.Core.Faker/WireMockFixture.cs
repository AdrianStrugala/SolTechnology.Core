using SolTechnology.Core.Faker.FakesBase;

namespace SolTechnology.Core.Faker
{
    public class WireMockFixture : IDisposable
    {
        private WireMockStartup _wireMockStartup = null!;

        public WireMockStartup Initialize(int port = 2137)
        {
            _wireMockStartup = new WireMockStartup();
            _wireMockStartup.Run(port);

            return _wireMockStartup;
        }

        public void RegisterFakeApi(IFakeApi fakeApi) => _wireMockStartup.RegisterFakeApi(fakeApi);

        public IFakeServiceBuilderWithRequest<T> Fake<T>() where T : class =>
            _wireMockStartup.GetFakeApi<T>();

        public void Dispose()
        {
            _wireMockStartup.WireMockServer.Reset();
        }
    }
}
