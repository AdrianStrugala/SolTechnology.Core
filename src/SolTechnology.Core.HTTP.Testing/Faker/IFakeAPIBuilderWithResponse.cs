using WireMock.ResponseBuilders;

namespace SolTechnology.Core.HTTP.Testing.Faker
{
    public interface IFakeApiBuilderWithResponse
    {
        public void WithResponse(Action<IResponseBuilder> configure);
    }
}
