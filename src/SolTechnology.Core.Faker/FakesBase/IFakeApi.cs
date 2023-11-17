using WireMock.Server;

namespace SolTechnology.Core.Faker.FakesBase
{
    public interface IFakeApi
    {
        public void Register(WireMockServer mockServer);
    }
}
