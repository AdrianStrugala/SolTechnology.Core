using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;

namespace SolTechnology.Core.Logging.Tests;

public class ModuleResolutionTests
{
    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Test]
    public async Task AddSolLogging_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolLogging(o => o.SkipPaths = ["/health"]);
        services.LogDetail("X-Tenant-Id", asName: "TenantId", source: LogDetailSource.Header);
        await using var provider = services.BuildServiceProvider(StrictValidation);

        // Act
        var resolved = await ResolveModuleServices(provider);

        // Assert
        resolved.StatusCode.Should().Be(StatusCodes.Status200OK);
        resolved.Services.Should().AllSatisfy(service => service.Should().NotBeNull());
    }

    [Test]
    public async Task AddSolLoggingWithConfiguration_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolLogging(BuildConfiguration());
        services.LogDetail("X-Tenant-Id", asName: "TenantId", source: LogDetailSource.Header);
        await using var provider = services.BuildServiceProvider(StrictValidation);

        // Act
        var resolved = await ResolveModuleServices(provider);

        // Assert
        resolved.StatusCode.Should().Be(StatusCodes.Status200OK);
        resolved.Services.Should().AllSatisfy(service => service.Should().NotBeNull());
    }

    [Test]
    public async Task AddSolLoggingWithConfiguration_RequestThroughUseSolLogging_Returns200()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddSolLogging(BuildConfiguration()))
                .Configure(app =>
                {
                    app.UseSolLogging();
                    app.Run(context => context.Response.WriteAsync("ok"));
                }))
            .StartAsync();

        // Act
        using var response = await host.GetTestClient().GetAsync("/api/trips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(CorrelationId.HeaderKey);
    }

    private static IConfiguration BuildConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{LoggingOptions.SectionName}:SkipPaths:0"] = "/health"
        })
        .Build();

    private static async Task<(int StatusCode, object[] Services)> ResolveModuleServices(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        var app = new ApplicationBuilder(scoped);
        app.UseSolLogging();
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext { RequestServices = scoped };
        context.Request.Path = "/api/trips";
        await app.Build()(context);

        object[] services =
        [
            scoped.GetRequiredService<ICorrelationIdService>(),
            scoped.GetRequiredService<ITimingService>(),
            scoped.GetRequiredService<IChronicle>(),
            scoped.GetRequiredService<TimeProvider>(),
            scoped.GetRequiredService<IOptions<LoggingOptions>>().Value,
            scoped.GetServices<ILogScopeEnricher>().ToArray()
        ];
        return (context.Response.StatusCode, services);
    }
}
