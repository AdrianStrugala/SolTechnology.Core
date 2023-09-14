using WireMock.ResponseBuilders;

namespace TaleCode.Faker.FakesBase
{
    public interface IFakeServiceBuilderWithResponse
    {
        public void WithResponse(Action<IResponseBuilder> configure);
    }
}
