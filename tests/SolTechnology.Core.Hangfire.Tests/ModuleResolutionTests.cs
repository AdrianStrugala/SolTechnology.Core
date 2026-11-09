using FluentAssertions;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Hangfire.Filters;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Hangfire.Tests;

[TestFixture]
public class ModuleResolutionTests
{
    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Test]
    [Ignore("infra#13: ICorrelationIdService is not registered by Hangfire; step 02 removes this Ignore.")]
    public async Task AddSolHangfire_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        await using var provider = CreateServices().BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var publisher = scoped.GetRequiredService<IEventPublisher>();
        var hostedServices = scoped.GetServices<IHostedService>();
        var job = scoped.GetRequiredService<TestJob>();
        var runner = scoped.GetRequiredService<RecurringJobRunner<TestJob>>();
        var correlationFilter = scoped.GetRequiredService<CorrelationIdJobFilter>();
        var smartRetryFilter = scoped.GetRequiredService<SmartRetryJobFilter>();
        var options = scoped.GetRequiredService<IOptions<PersistentEventsOptions>>().Value;

        // Assert
        publisher.Should().BeOfType<HangfireEventPublisher>();
        hostedServices.Should().ContainSingle(s => s is RecurringJobRegistrar);
        job.Should().NotBeNull();
        runner.Should().NotBeNull();
        correlationFilter.Should().NotBeNull();
        smartRetryFilter.Should().NotBeNull();
        options.QueueName.Should().Be("default");
    }

    [Test]
    [Ignore("infra#13: ICorrelationIdService is not registered by Hangfire; step 02 removes this Ignore.")]
    public async Task AddSolHangfire_WithoutAddSolLogging_RegistersCorrelationIdService()
    {
        // Arrange
        await using var provider = CreateServices().BuildServiceProvider();

        // Act
        var correlationIdService = provider.GetService<ICorrelationIdService>();

        // Assert
        correlationIdService.Should().NotBeNull();
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddHangfire(c => c.UseInMemoryStorage());
        services.AddSolCQRS(o => o.RegisterEventsFromAssembly(typeof(ModuleResolutionTests).Assembly));
        services.AddSolPersistentEvents();
        services.AddSolRecurringJob<TestJob>("0 0 * * *");
        return services;
    }
}
