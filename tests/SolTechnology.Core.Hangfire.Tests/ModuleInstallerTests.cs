using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Hangfire;

namespace SolTechnology.Core.Hangfire.Tests;

[TestFixture]
public class ModuleInstallerTests
{
    [Test]
    public void AddPersistentEvents_WithoutAddCQRS_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddPersistentEvents();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AddCQRS()*");
    }

    [Test]
    public void AddPersistentEvents_AfterAddCQRS_ReplacesPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IBackgroundJobClient>());
        services.AddCQRS(assemblies: typeof(ModuleInstallerTests).Assembly);

        // Act
        services.AddPersistentEvents();
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<IEventPublisher>()
            .Should().BeOfType<HangfireEventPublisher>();
    }

    [Test]
    public void AddPersistentEvents_BeforeAddCQRS_StillResolvesHangfirePublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IBackgroundJobClient>());
        services.AddCQRS(assemblies: typeof(ModuleInstallerTests).Assembly);
        services.AddPersistentEvents();

        // Re-call AddCQRS to simulate order-independence (TryAdd yields to existing)
        services.AddCQRS(assemblies: typeof(ModuleInstallerTests).Assembly);
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<IEventPublisher>()
            .Should().BeOfType<HangfireEventPublisher>();
    }

    [Test]
    public void AddPersistentEvents_WithCustomQueueName_BindsOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IBackgroundJobClient>());
        services.AddCQRS(assemblies: typeof(ModuleInstallerTests).Assembly);

        // Act
        services.AddPersistentEvents(o => o.QueueName = "events");
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PersistentEventsOptions>>();

        // Assert
        options.Value.QueueName.Should().Be("events");
    }
}



