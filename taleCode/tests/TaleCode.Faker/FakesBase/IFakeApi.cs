using WireMock.Server;

namespace TaleCode.Faker.FakesBase
{
    public interface IFakeApi
    {
        public void Register(WireMockServer mockServer);
    }
}
