using SolTechnology.Core.Faker.WireMock;

// ReSharper disable once CheckNamespace
namespace WireMock.Server;

public static class WireMockExtensions
{
    public static IWireMockFakerConfigurator<TClient> CreateFor<TClient>(this WireMockServer server)
        where TClient : class => new WireMockFakerConfigurator<TClient>(server);
}
