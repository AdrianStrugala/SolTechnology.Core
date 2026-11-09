using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Tests;

public sealed class ModuleInstallerEndpointTests
{
    private sealed record FooMessage(string Id) : IMessage;
    private sealed record BarMessage(string Id) : IMessage;

    private sealed class FooHandler : IMessageHandler<FooMessage>
    {
        public Task Handle(FooMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class BarHandler : IMessageHandler<BarMessage>
    {
        public Task Handle(BarMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static IServiceCollection BaseServices() =>
        new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

    [Test]
    public void WithEndpoints_SeveralRegistrations_CollectedInOneRegistry()
    {
        // Act
        var services = BaseServices()
            .AddSolMessageBus(new MessageBusConfiguration
            {
                ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
            })
            .WithQueuePublisher<FooMessage>("foo-queue")
            .WithQueueReceiver<FooMessage, FooHandler>("foo-queue")
            .WithTopicPublisher<BarMessage>("bar-topic")
            .WithTopicReceiver<BarMessage, BarHandler>("bar-topic", "bar-sub");

        // Assert
        var registry = services
            .Single(d => d.ServiceType == typeof(MessageBusRegistry))
            .ImplementationInstance.Should().BeOfType<MessageBusRegistry>().Subject;
        registry.Endpoints.Should().HaveCount(4);
        registry.Endpoints.Should().Contain(e =>
            e.MessageType == typeof(FooMessage) &&
            e.Kind == EndpointKind.Queue &&
            e.Role == EndpointRole.Publisher &&
            e.EntityName == "foo-queue");
        registry.Endpoints.Should().Contain(e =>
            e.MessageType == typeof(BarMessage) &&
            e.Kind == EndpointKind.Topic &&
            e.Role == EndpointRole.Receiver &&
            e.EntityName == "bar-topic" &&
            e.SubscriptionName == "bar-sub");
    }

    [Test]
    public void WithQueueReceiver_QueueNameOmitted_ResolvedFromConfiguration()
    {
        // Act
        var services = BaseServices()
            .AddSolMessageBus(new MessageBusConfiguration
            {
                ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
                Queues = new List<QueueConfiguration>
                {
                    new() { MessageType = nameof(FooMessage), QueueName = "foo-from-config" }
                }
            })
            .WithQueueReceiver<FooMessage, FooHandler>();

        // Assert
        var registry = services
            .Single(d => d.ServiceType == typeof(MessageBusRegistry))
            .ImplementationInstance as MessageBusRegistry;
        registry!.Endpoints.Should().ContainSingle()
            .Which.EntityName.Should().Be("foo-from-config");
    }

    [Test]
    public void WithQueueReceiver_QueueNameUnresolvable_Throws()
    {
        // Arrange
        var services = BaseServices()
            .AddSolMessageBus(new MessageBusConfiguration
            {
                ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
            });

        // Act
        var act = () => services.WithQueueReceiver<FooMessage, FooHandler>();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*FooMessage*");
    }
}
