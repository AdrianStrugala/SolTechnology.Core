using System;
using SolTechnology.Core.Faker;
using SolTechnology.Core.Faker.FakesBase;
using TaleCode.FunctionalTests.FakeApis;

namespace TaleCode.FunctionalTests.TestsConfiguration
{
    public class WireMockFixture : IDisposable
    {
        private WireMockStartup _wireMockStartup = null!;

        public WireMockStartup Initialize(int port = 2137)
        {
            _wireMockStartup = new WireMockStartup();
            _wireMockStartup.Run(port);
            _wireMockStartup.RegisterFakeApi(new ApiFootballFakeApi());
            _wireMockStartup.RegisterFakeApi(new FootballDataFakeApi());

            return _wireMockStartup;
        }

        public IFakeServiceBuilderWithRequest<T> Fake<T>() where T : class =>
            _wireMockStartup.GetFakeApi<T>();

        public void Dispose()
        {
            _wireMockStartup.WireMockServer.Reset();
        }
    }
}
