using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.HTTP.Tests;

[TestFixture]
public class ModuleResolutionTests
{
    private const string ClientName = "Sample";

    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    public sealed class SampleClientOptions
    {
        public string? ApiKey { get; set; }
    }

    [Test]
    public async Task AddSolHTTPClient_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(BuildConfiguration());
        services.AddSolHTTPClient<ISampleHTTPClient, SampleHTTPClient>(ClientName);
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var resolved = ResolveModuleServices(scoped);

        // Assert
        resolved.Client.HttpClient.BaseAddress.Should().Be(new Uri("http://upstream.test/"));
        resolved.Services.Should().AllSatisfy(service => service.Should().NotBeNull());
        scoped.GetService<ICorrelationIdService>().Should().NotBeNull();
    }

    [Test]
    public async Task AddSolHTTPClientWithOptions_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(BuildConfiguration());
        services.AddSolHTTPClient<ISampleHTTPClient, SampleHTTPClient, SampleClientOptions>(ClientName, propagateCorrelation: false);
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var resolved = ResolveModuleServices(scoped);
        var options = scoped.GetRequiredService<IOptions<SampleClientOptions>>().Value;

        // Assert
        resolved.Client.HttpClient.BaseAddress.Should().Be(new Uri("http://upstream.test/"));
        resolved.Services.Should().AllSatisfy(service => service.Should().NotBeNull());
        options.ApiKey.Should().Be("key-1");
    }

    private static IConfiguration BuildConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{HTTPClientConfiguration.SectionName}:{ClientName}:BaseAddress"] = "http://upstream.test/",
            [$"{HTTPClientConfiguration.SectionName}:{ClientName}:{HTTPClientConfiguration.OptionsSubSection}:ApiKey"] = "key-1"
        })
        .Build();

    private static (ISampleHTTPClient Client, object[] Services) ResolveModuleServices(IServiceProvider scoped)
    {
        scoped.GetRequiredService<IStartupValidator>().Validate();

        var client = scoped.GetRequiredService<ISampleHTTPClient>();
        object[] services =
        [
            client,
            scoped.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName),
            scoped.GetRequiredService<IOptionsMonitor<HTTPClientConfiguration>>().Get(ClientName),
            scoped.GetRequiredService<IOptionsMonitor<HttpPolicyConfiguration>>().Get(ClientName)
        ];
        return (client, services);
    }
}
