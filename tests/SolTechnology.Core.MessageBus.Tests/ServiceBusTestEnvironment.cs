using NUnit.Framework;
using SolTechnology.Core.ServiceBus.Testing;

namespace SolTechnology.Core.MessageBus.Tests;

[SetUpFixture]
public static class ServiceBusTestEnvironment
{
    private static readonly Lazy<Task<ServiceBusFixture>> Fixture = new(StartFixture);

    public static async Task<string> ConnectionString() => (await Fixture.Value).ConnectionString;

    [OneTimeTearDown]
    public static async Task TearDown()
    {
        if (Fixture.IsValueCreated && Fixture.Value.IsCompletedSuccessfully)
        {
            await Fixture.Value.Result.DisposeAsync();
        }
    }

    private static async Task<ServiceBusFixture> StartFixture()
    {
        var fixture = new ServiceBusFixture(
            containerName: "soltech-messagebus-tests-servicebus",
            configFilePath: Path.Combine(AppContext.BaseDirectory, "servicebus-emulator-config.json"));
        await fixture.Initialize();
        return fixture;
    }
}
