using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.PipelineBehaviors;

namespace SolTechnology.Core.CQRS.Tests.Registration;

[TestFixture]
public class AddSolCQRSTests
{
    [Test]
    public void AddSolCQRS_CalledTwice_RegistersBehaviorsOnlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSolCQRS(o => o
            .RegisterCommandsFromAssembly(typeof(AddSolCQRSTests).Assembly)
            .RegisterQueriesFromAssembly(typeof(AddSolCQRSTests).Assembly)
            .RegisterEventsFromAssembly(typeof(AddSolCQRSTests).Assembly));
        services.AddSolCQRS(o => o
            .RegisterCommandsFromAssembly(typeof(AddSolCQRSTests).Assembly)
            .RegisterQueriesFromAssembly(typeof(AddSolCQRSTests).Assembly)
            .RegisterEventsFromAssembly(typeof(AddSolCQRSTests).Assembly));

        // Assert
        var behaviorRegistrations = services
            .Where(s => s.ServiceType.IsGenericType &&
                        s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorRegistrations.Should().HaveCount(2); // Logging + Validation, each once
    }

    [Test]
    public void AddSolCQRS_WithValidationDisabled_DoesNotRegisterValidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSolCQRS(o =>
        {
            o.UseFluentValidation = false;
            o.RegisterQueriesFromAssembly(typeof(AddSolCQRSTests).Assembly);
        });

        // Assert
        var behaviorRegistrations = services
            .Where(s => s.ServiceType.IsGenericType &&
                        s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorRegistrations.Should().HaveCount(1);
        behaviorRegistrations[0].ImplementationType!.GetGenericTypeDefinition()
            .Should().Be(typeof(LoggingPipelineBehavior<,>));
    }

    [Test]
    public void AddSolCQRS_RegistersMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSolCQRS(o => o
            .RegisterCommandsFromAssembly(typeof(AddSolCQRSTests).Assembly)
            .RegisterQueriesFromAssembly(typeof(AddSolCQRSTests).Assembly)
            .RegisterEventsFromAssembly(typeof(AddSolCQRSTests).Assembly));

        // Assert
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Test]
    public void AddSolCQRS_RegistersHandlersFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSolCQRS(o => o
            .RegisterCommandsFromAssembly(typeof(AddSolCQRSTests).Assembly)
            .RegisterQueriesFromAssembly(typeof(AddSolCQRSTests).Assembly)
            .RegisterEventsFromAssembly(typeof(AddSolCQRSTests).Assembly));

        // Assert
        var handlerRegistration = services
            .FirstOrDefault(s => s.ServiceType == typeof(IQueryHandler<TestQuery, string>));

        handlerRegistration.Should().NotBeNull();
    }

    [Test]
    public void AddSolCQRS_WithoutDiscovery_RegistersMediatorButNoHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act — no lambda: infrastructure only, explicit discovery skipped
        services.AddSolCQRS();

        // Assert
        services.Any(s => s.ServiceType == typeof(IMediator)).Should().BeTrue();

        var handlerRegistrations = services.Where(s =>
            s.ServiceType.IsGenericType &&
            (s.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
             s.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
             s.ServiceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
             s.ServiceType.GetGenericTypeDefinition() == typeof(IEventHandler<>)));

        handlerRegistrations.Should().BeEmpty();
    }
}

