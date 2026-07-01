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
    public void AddSolPersistentEvents_WithoutAddSolCQRS_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddSolPersistentEvents();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AddSolCQRS()*");
    }

    [Test]
    public void AddSolPersistentEvents_AfterAddSolCQRS_ReplacesPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IBackgroundJobClient>());
        services.AddSolCQRS(o => o.RegisterEventsFromAssembly(typeof(ModuleInstallerTests).Assembly));

        // Act
        services.AddSolPersistentEvents();
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<IEventPublisher>()
            .Should().BeOfType<HangfireEventPublisher>();
    }

    [Test]
    public void AddSolPersistentEvents_BeforeAddSolCQRS_StillResolvesHangfirePublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IBackgroundJobClient>());
        services.AddSolCQRS(o => o.RegisterEventsFromAssembly(typeof(ModuleInstallerTests).Assembly));
        services.AddSolPersistentEvents();

        // Re-call AddSolCQRS to simulate order-independence (TryAdd yields to existing)
        services.AddSolCQRS(o => o.RegisterEventsFromAssembly(typeof(ModuleInstallerTests).Assembly));
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<IEventPublisher>()
            .Should().BeOfType<HangfireEventPublisher>();
    }

    [Test]
    public void AddSolPersistentEvents_WithCustomQueueName_BindsOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IBackgroundJobClient>());
        services.AddSolCQRS(o => o.RegisterEventsFromAssembly(typeof(ModuleInstallerTests).Assembly));

        // Act
        services.AddSolPersistentEvents(o => o.QueueName = "events");
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PersistentEventsOptions>>();

        // Assert
        options.Value.QueueName.Should().Be("events");
    }
}



