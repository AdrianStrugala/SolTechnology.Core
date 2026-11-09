using Docker.DotNet;
using DotNet.Testcontainers.Configurations;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Testing.Containers;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class TestContainersContextTests
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task DisposeAsync_WithOrWithoutRyuk_RemovesTheNetwork(bool ryukEnabled)
    {
        Assume.That(TestContainersContext.ReuseContainers, Is.False, "reuse keeps the network by design");

        // Arrange
        var original = TestcontainersSettings.ResourceReaperEnabled;
        TestcontainersSettings.ResourceReaperEnabled = ryukEnabled;
        string networkName;
        try
        {
            var context = new TestContainersContext();
            await context.Network.CreateAsync();
            networkName = context.Network.Name;

            // Act
            await context.DisposeAsync();
        }
        finally
        {
            TestcontainersSettings.ResourceReaperEnabled = original;
        }

        // Assert
        using var docker = new DockerClientBuilder().Build();
        var networks = await docker.Networks.ListNetworksAsync();
        networks.Select(n => n.Name).Should().NotContain(networkName);
    }
}
