using TaleCode.Faker.FakesBase;
using WireMock.RequestBuilders;
using WireMock.Server;

namespace TaleCode.Faker.WireMock;

public interface IWireMockFakerConfigurator<TClient> where TClient : class
{
    IWireMockFakerConfigurator<TClient> WithBaseUrl(string url);

    public IRespondWithAProvider BuildRequest(RequestInfo requestInfo, Action<IRequestBuilder>? configure = null);
}