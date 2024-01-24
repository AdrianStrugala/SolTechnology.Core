using WireMock.ResponseBuilders;

namespace SolTechnology.Core.Faker.FakesBase
{
    public interface IFakeServiceBuilderWithResponse
    {
        public void WithResponse(Action<IResponseBuilder> configure);
    }
}
