using WireMock.RequestBuilders;
using WireMock.Server;

namespace SolTechnology.Core.HTTP.Testing.Faker;

/// <summary>
/// Base class of a fake HTTP API. Derive from it, implement your client interface, and in each method build the
/// request matcher and pass it to <see cref="BuildRequest"/>; <see cref="WireMockFixture.Fake{T}"/> then attaches the response.
/// </summary>
public abstract class FakeApiBase : IFakeApi
{
    // Set by the fake's interface-method bodies (via BuildRequest) and read by FakeApiBuilder<T> when
    // attaching the response. `protected` so the derived fake (another assembly) can assign it;
    // `internal` so the builder (this assembly) can read it.
    /// <summary>The stub the last fake method call arranged; set it to the result of <see cref="BuildRequest"/>.</summary>
    protected internal IRespondWithAProvider Provider = null!;
    private WireMockServer _mockServer = null!;

    /// <inheritdoc />
    public void Register(WireMockServer mockServer)
    {
        _mockServer = mockServer;
    }


    /// <summary>Registers <paramref name="request"/> as a stub on the server and returns it, ready for a response.</summary>
    protected IRespondWithAProvider BuildRequest(IRequestBuilder request)
    {
        return _mockServer.Given(request);
    }
}
