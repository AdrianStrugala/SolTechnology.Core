using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.HealthChecks;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Tests;

[TestFixture]
public class ModuleResolutionTests
{
    private const string UnreachableServiceBus =
        "Endpoint=sb://resolution-test.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v";

    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    private sealed record ResolutionMessage(string Id) : IMessage;

    private sealed class ResolutionHandler : IMessageHandler<ResolutionMessage>
    {
        public Task Handle(ResolutionMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Test]
    public async Task AddSolMessageBus_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services
            .AddSolMessageBus(new MessageBusConfiguration { ConnectionString = UnreachableServiceBus })
            .WithQueuePublisher<ResolutionMessage>("resolution-queue")
            .WithQueueReceiver<ResolutionMessage, ResolutionHandler>("resolution-queue");
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var publisher = scoped.GetRequiredService<IMessagePublisher>();
        var hostedServices = scoped.GetServices<IHostedService>();
        var handler = scoped.GetRequiredService<IMessageHandler<ResolutionMessage>>();
        var options = scoped.GetRequiredService<IOptions<MessageBusConfiguration>>().Value;

        // Assert
        publisher.Should().NotBeNull();
        hostedServices.Should().ContainSingle(service => service.GetType().Name == "MessageBusReceiver");
        handler.Should().BeOfType<ResolutionHandler>();
        options.ConnectionString.Should().Be(UnreachableServiceBus);
    }

    [Test]
    public async Task AddSolServiceBusHealthCheck_RegistrationFactory_Resolves()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolMessageBus(new MessageBusConfiguration { ConnectionString = UnreachableServiceBus });
        services.AddHealthChecks().AddSolServiceBusHealthCheck();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var registration = scope.ServiceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value
            .Registrations
            .Single(r => r.Name == "servicebus");

        // Act
        var healthCheck = registration.Factory(scope.ServiceProvider);

        // Assert
        healthCheck.Should().NotBeNull();
    }

    [Test]
    public async Task AddSolMessageBus_WithoutLogging_RegistersCorrelationIdService()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolMessageBus(new MessageBusConfiguration { ConnectionString = UnreachableServiceBus });
        await using var provider = services.BuildServiceProvider();

        // Act
        var correlationIdService = provider.GetService<ICorrelationIdService>();

        // Assert
        correlationIdService.Should().NotBeNull();
    }
}
