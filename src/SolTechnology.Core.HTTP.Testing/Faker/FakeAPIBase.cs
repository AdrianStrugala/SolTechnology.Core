using WireMock.RequestBuilders;
using WireMock.Server;

namespace SolTechnology.Core.HTTP.Testing.Faker;

public abstract class FakeApiBase : IFakeApi
{
    // Set by the fake's interface-method bodies (via BuildRequest) and read by FakeApiBuilder<T> when
    // attaching the response. `protected` so the derived fake (another assembly) can assign it;
    // `internal` so the builder (this assembly) can read it.
    protected internal IRespondWithAProvider Provider = null!;
    private WireMockServer _mockServer = null!;

    protected abstract string BaseUrl { get; }

    public void Register(WireMockServer mockServer)
    {
        _mockServer = mockServer;
    }


    protected IRespondWithAProvider BuildRequest(IRequestBuilder request)
    {
        return _mockServer.Given(request);
    }
}
