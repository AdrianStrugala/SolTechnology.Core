using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace SolTechnology.Core.Cache.Tests;

[TestFixture]
public class ModuleInstallerTests
{
    private const string UnreachableRedis = "localhost:1,abortConnect=false,connectTimeout=100";

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task AddSolDistributedCache_MissingInstanceName_HostStartFails(string? instanceName)
    {
        // Arrange
        var configuration = new DistributedCacheConfiguration { ConnectionString = UnreachableRedis };
        if (instanceName is not null)
        {
            configuration.InstanceName = instanceName;
        }
        using var host = BuildHost(services => services.AddSolDistributedCache(configuration));

        // Act
        var start = async () => await host.StartAsync();

        // Assert
        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain(nameof(DistributedCacheConfiguration.InstanceName));
    }

    [Test]
    public async Task AddSolLocalCache_ConfigWithoutExpirationSeconds_HostStartFails()
    {
        // Arrange
        var configuration = new CacheConfiguration { ExpirationMode = ExpirationMode.Sliding };
        using var host = BuildHost(services => services.AddSolLocalCache(configuration));

        // Act
        var start = async () => await host.StartAsync();

        // Assert
        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain(nameof(CacheConfiguration.ExpirationSeconds));
    }

    private static IHost BuildHost(Action<IServiceCollection> configure)
    {
        var builder = Host.CreateApplicationBuilder();
        configure(builder.Services);
        return builder.Build();
    }
}
