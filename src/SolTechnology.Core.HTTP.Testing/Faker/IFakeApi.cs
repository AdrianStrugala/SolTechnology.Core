using WireMock.Server;

namespace SolTechnology.Core.HTTP.Testing.Faker
{
    public interface IFakeApi
    {
        public void Register(WireMockServer mockServer);
    }
}
