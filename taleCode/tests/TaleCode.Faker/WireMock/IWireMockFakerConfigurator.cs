using TaleCode.Faker.FakesBase;
using WireMock.RequestBuilders;
using WireMock.Server;

namespace TaleCode.Faker.WireMock;

public interface IWireMockFakerConfigurator<TClient> where TClient : class
{
    IWireMockFakerConfigurator<TClient> WithBaseUrl(string url);

    public IRespondWithAProvider BuildRequest(RequestInfo requestInfo, Dictionary<string, string>? parameters, Action<IRequestBuilder>? configure = null);
}