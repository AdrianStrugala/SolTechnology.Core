﻿using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.MessageBus;
using SolTechnology.Core.MessageBus.Broker;
using SolTechnology.Core.MessageBus.Configuration;
using SolTechnology.Core.MessageBus.Publish;

namespace SolTechnology.Core.MessageBus.Tests;

public sealed class ModuleInstallerTests
{
    private const string ConnectionString =
        "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v";

    private static IServiceCollection BaseServices() =>
        new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

    [Test]
    public void AddSolMessageBus_ValidConfiguration_RegistersRequiredServices()
    {
        // Act
        var services = BaseServices()
            .AddSolMessageBus(new MessageBusConfiguration
            {
                ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
            });

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(IMessagePublisher));
        services.Should().Contain(d => d.ServiceType == typeof(IMessageBusBroker));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IConfigureOptions<MessageBusConfiguration>));
    }

    [Test]
    public void AddSolMessageBus_NullConfiguration_Throws()
    {
        // Act
        var act = () => BaseServices().AddSolMessageBus(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestCase("", 10, "*ConnectionString*")]
    [TestCase("   ", 10, "*ConnectionString*")]
    [TestCase(ConnectionString, 0, "*MaxDeliveryCount*")]
    [TestCase(ConnectionString, -1, "*MaxDeliveryCount*")]
    public async Task AddSolMessageBus_InvalidOptions_HostStartFails(
        string connectionString, int maxDeliveryCount, string expectedMessage)
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSolMessageBus(new MessageBusConfiguration
        {
            ConnectionString = connectionString,
            MaxDeliveryCount = maxDeliveryCount
        });
        using var host = builder.Build();

        // Act
        var act = () => host.StartAsync();

        // Assert
        await act.Should().ThrowAsync<OptionsValidationException>().WithMessage(expectedMessage);
    }

    [Test]
    public async Task AddSolMessageBus_ConsumerRegisteredServiceBusClient_IsNotReused()
    {
        // Arrange
        await using var consumerClient = new ServiceBusClient(ConnectionString);
        var services = BaseServices()
            .AddSingleton(consumerClient)
            .AddSolMessageBus(new MessageBusConfiguration { ConnectionString = ConnectionString });
        await using var provider = services.BuildServiceProvider();

        // Act
        var sharedClient = provider.GetRequiredKeyedService<ServiceBusClient>(MessageBusClientFactory.ServiceKey);
        var consumerResolved = provider.GetRequiredService<ServiceBusClient>();

        // Assert
        sharedClient.Should().NotBeSameAs(consumerClient);
        consumerResolved.Should().BeSameAs(consumerClient);
    }

    [Test]
    public async Task AddSolMessageBus_Client_UsesConfiguredTransportAndRetry()
    {
        // Arrange
        var services = BaseServices().AddSolMessageBus(new MessageBusConfiguration
        {
            ConnectionString = ConnectionString,
            TransportType = ServiceBusTransportType.AmqpTcp,
            RetryOptions = new ServiceBusRetryOptions { MaxRetries = 7, Mode = ServiceBusRetryMode.Fixed }
        });
        await using var provider = services.BuildServiceProvider();

        // Act
        var client = provider.GetRequiredKeyedService<ServiceBusClient>(MessageBusClientFactory.ServiceKey);
        var clientOptions = MessageBusClientFactory.CreateOptions(
            provider.GetRequiredService<IOptions<MessageBusConfiguration>>().Value);

        // Assert
        client.TransportType.Should().Be(ServiceBusTransportType.AmqpTcp);
        clientOptions.TransportType.Should().Be(ServiceBusTransportType.AmqpTcp);
        clientOptions.RetryOptions.MaxRetries.Should().Be(7);
        clientOptions.RetryOptions.Mode.Should().Be(ServiceBusRetryMode.Fixed);
    }

    [Test]
    public async Task Broker_DisposeAsync_DoesNotDisposeSharedClient()
    {
        // Arrange
        var services = BaseServices()
            .AddSolMessageBus(new MessageBusConfiguration { ConnectionString = ConnectionString });
        await using var provider = services.BuildServiceProvider();
        var sharedClient = provider.GetRequiredKeyedService<ServiceBusClient>(MessageBusClientFactory.ServiceKey);
        var broker = (IAsyncDisposable)provider.GetRequiredService<IMessageBusBroker>();

        // Act
        await broker.DisposeAsync();

        // Assert
        sharedClient.IsClosed.Should().BeFalse();
    }
}
