using Azure.Messaging.ServiceBus;
using Docker.DotNet;
using Docker.DotNet.Models;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.ServiceBus.Testing;
using SolTechnology.Core.Testing.Containers;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class ServiceBusFixtureLifecycleTests
{
    [Test]
    public async Task DisposeAsync_FirstOfTwoFixturesWithTheSameName_SecondStillWorksAndFirstLeavesNoContainer()
    {
        Assume.That(TestContainersContext.ReuseContainers, Is.False, "reuse keeps the containers by design");

        // Arrange
        const string name = "soltech-messagebus-tests-lifecycle";
        await using var first = new ServiceBusFixture(name);
        await using var second = new ServiceBusFixture(name);
        await first.Initialize();
        await second.Initialize();
        var firstConnectionString = first.ConnectionString;
        var (firstContainers, firstNetworks) = await Resources(first.Container!.Id);

        // Act
        await first.DisposeAsync();

        // Assert
        second.ConnectionString.Should().NotBe(firstConnectionString);
        await using var client = new ServiceBusClient(second.ConnectionString);
        await client.CreateSender("queue.1").Invoking(s => s.SendMessageAsync(new ServiceBusMessage("ping")))
            .Should().NotThrowAsync();
        firstContainers.Should().HaveCount(2, "the emulator and its MSSQL sidecar share the network");
        (await ReceiverTestHost.Eventually(async () => (await Existing(firstContainers, firstNetworks)).Count == 0))
            .Should().BeTrue("the first fixture removes its emulator, sidecar and network");
    }

    private static async Task<(IReadOnlyList<string> Containers, IReadOnlyList<string> Networks)> Resources(string emulatorId)
    {
        using var docker = new DockerClientBuilder().Build();
        var emulator = await docker.Containers.InspectContainerAsync(emulatorId);
        var networks = emulator.NetworkSettings?.Networks?.Values.Select(n => n.NetworkID).ToList() ?? [];
        var containers = await docker.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["network"] = networks.ToDictionary(id => id, _ => true)
            }
        });

        return (containers.Select(c => c.ID).ToList(), networks);
    }

    private static async Task<IReadOnlyList<string>> Existing(IReadOnlyList<string> containers, IReadOnlyList<string> networks)
    {
        using var docker = new DockerClientBuilder().Build();
        var allContainers = await docker.Containers.ListContainersAsync(new ContainersListParameters { All = true });
        var allNetworks = await docker.Networks.ListNetworksAsync();

        return allContainers.Select(c => c.ID).Intersect(containers)
            .Concat(allNetworks.Select(n => n.ID).Intersect(networks))
            .ToList();
    }
}
