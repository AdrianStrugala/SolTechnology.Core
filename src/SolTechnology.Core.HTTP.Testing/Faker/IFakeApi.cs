using WireMock.Server;

namespace SolTechnology.Core.HTTP.Testing.Faker
{
    /// <summary>A fake HTTP API that registers its stubs on a WireMock server; derive from <see cref="FakeApiBase"/>.</summary>
    public interface IFakeApi
    {
        /// <summary>Gives the fake the server its stubs are registered on.</summary>
        public void Register(WireMockServer mockServer);
    }
}
