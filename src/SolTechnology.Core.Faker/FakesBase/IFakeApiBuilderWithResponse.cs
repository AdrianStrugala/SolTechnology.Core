using WireMock.ResponseBuilders;

namespace SolTechnology.Core.Faker.FakesBase
{
    public interface IFakeApiBuilderWithResponse
    {
        public void WithResponse(Action<IResponseBuilder> configure);
    }
}
