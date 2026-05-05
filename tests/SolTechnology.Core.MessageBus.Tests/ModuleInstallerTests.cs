﻿using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.MessageBus;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Tests;

public sealed class ModuleInstallerTests
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
    public void AddMessageBus_registers_required_services()
    {
        var services = BaseServices()
            .AddMessageBus(new MessageBusConfiguration
            {
                ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
            });

        services.Should().Contain(d => d.ServiceType == typeof(IMessagePublisher));
        services.Should().Contain(d => d.ServiceType == typeof(IMessageBusBroker));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IConfigureOptions<MessageBusConfiguration>));
    }

    [Test]
    public void With_methods_collect_endpoints_into_a_single_registry()
    {
        var services = BaseServices()
            .AddMessageBus(new MessageBusConfiguration
            {
                ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
            })
            .WithQueuePublisher<FooMessage>("foo-queue")
            .WithQueueReceiver<FooMessage, FooHandler>("foo-queue")
            .WithTopicPublisher<BarMessage>("bar-topic")
            .WithTopicReceiver<BarMessage, BarHandler>("bar-topic", "bar-sub");

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
    public void WithQueueReceiver_resolves_queue_name_from_configuration_when_omitted()
    {
        var services = BaseServices()
            .AddMessageBus(new MessageBusConfiguration
            {
                ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
                Queues = new List<QueueConfiguration>
                {
                    new() { MessageType = nameof(FooMessage), QueueName = "foo-from-config" }
                }
            })
            .WithQueueReceiver<FooMessage, FooHandler>();

        var registry = services
            .Single(d => d.ServiceType == typeof(MessageBusRegistry))
            .ImplementationInstance as MessageBusRegistry;

        registry!.Endpoints.Should().ContainSingle()
            .Which.EntityName.Should().Be("foo-from-config");
    }

    [Test]
    public void WithQueueReceiver_throws_when_queue_name_cannot_be_resolved()
    {
        var services = BaseServices()
            .AddMessageBus(new MessageBusConfiguration
            {
                ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
            });

        var act = () => services.WithQueueReceiver<FooMessage, FooHandler>();

        act.Should().Throw<ArgumentException>()
            .WithMessage("*FooMessage*");
    }

    [Test]
    public void AddMessageBus_throws_when_configuration_is_null()
    {
        var act = () => BaseServices().AddMessageBus(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}




