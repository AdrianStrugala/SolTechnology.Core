using SolTechnology.Core.HTTP.Testing.WireMock;
using WireMock.ResponseBuilders;

namespace SolTechnology.Core.HTTP.Testing.Faker;

/// <summary>
/// Strongly-typed arrange surface returned by <c>WireMockFixture.Fake&lt;TApiClient&gt;()</c>. Holds the
/// <typeparamref name="TApiClient"/> generic so the fake itself doesn't have to — the fake only implements
/// the client interface. The lambda calls the real fake method directly: full IntelliSense + compile-time
/// argument checking, no reflection.
/// </summary>
internal sealed class FakeApiBuilder<TApiClient>(FakeApiBase fake)
    : IFakeApiBuilderWithRequest<TApiClient>, IFakeApiBuilderWithResponse
    where TApiClient : class
{
    public IFakeApiBuilderWithResponse WithRequest(Action<TApiClient> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if ((object)fake is not TApiClient client)
        {
            throw new InvalidOperationException(
                $"{fake.GetType().Name} must implement {typeof(TApiClient).Name} to use WithRequest(...).");
        }

        // Runs the fake's interface-method body, which sets up the matcher and assigns fake.Provider.
        request(client);
        return this;
    }

    public void WithResponse(Action<IResponseBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = Response.Create();
        configure(new JsonResponseBuilderDecorator(builder));
        fake.Provider.RespondWith(builder);
    }
}


