using WireMock.ResponseBuilders;

namespace SolTechnology.Core.HTTP.Testing.Faker
{
    /// <summary>Second stage of arranging a fake: the response the server returns for the chosen request.</summary>
    public interface IFakeApiBuilderWithResponse
    {
        /// <summary>Configures the response; <c>WithBodyAsJson</c> serializes with <c>System.Text.Json</c>.</summary>
        public void WithResponse(Action<IResponseBuilder> configure);
    }
}
