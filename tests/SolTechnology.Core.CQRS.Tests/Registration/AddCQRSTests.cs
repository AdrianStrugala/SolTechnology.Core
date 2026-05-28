using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.PipelineBehaviors;

namespace SolTechnology.Core.CQRS.Tests.Registration;

[TestFixture]
public class AddCQRSTests
{
    [Test]
    public void AddCQRS_CalledTwice_RegistersBehaviorsOnlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS(assemblies: typeof(AddCQRSTests).Assembly);
        services.AddCQRS(assemblies: typeof(AddCQRSTests).Assembly);

        // Assert
        var behaviorRegistrations = services
            .Where(s => s.ServiceType.IsGenericType &&
                        s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorRegistrations.Should().HaveCount(2); // Logging + Validation, each once
    }

    [Test]
    public void AddCQRS_WithValidationDisabled_DoesNotRegisterValidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS(o => o.UseFluentValidation = false, typeof(AddCQRSTests).Assembly);

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
    public void AddCQRS_RegistersMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS(assemblies: typeof(AddCQRSTests).Assembly);

        // Assert
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Test]
    public void AddCQRS_RegistersHandlersFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS(assemblies: typeof(AddCQRSTests).Assembly);

        // Assert
        var handlerRegistration = services
            .FirstOrDefault(s => s.ServiceType == typeof(IQueryHandler<TestQuery, string>));

        handlerRegistration.Should().NotBeNull();
    }
}

