using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.CQRS.Tests;

[TestFixture]
public class ModuleResolutionTests
{
    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Test]
    public async Task AddSolCQRS_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<NotificationLog>();
        services.AddSolCQRS(o => o
            .RegisterCommandsFromAssembly(typeof(ModuleResolutionTests).Assembly)
            .RegisterQueriesFromAssembly(typeof(ModuleResolutionTests).Assembly)
            .RegisterEventsFromAssembly(typeof(ModuleResolutionTests).Assembly));
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var resolved = new object[]
        {
            scoped.GetRequiredService<IMediator>(),
            scoped.GetRequiredService<IEventPublisher>(),
            scoped.GetRequiredService<IEventDispatcher>(),
            scoped.GetRequiredService<ICommandHandler<TestCommand>>(),
            scoped.GetRequiredService<ICommandHandler<TestCommandWithResult, int>>(),
            scoped.GetRequiredService<IQueryHandler<TestQuery, string>>(),
            scoped.GetRequiredService<IValidator<ValidatedCommand>>()
        };
        var eventHandlers = scoped.GetServices<IEventHandler<TestNotification>>();
        var behaviors = scoped.GetServices<IPipelineBehavior<TestQuery, Result<string>>>();
        var queryResult = await scoped.GetRequiredService<IMediator>().Send(new TestQuery { Input = "DI" });

        // Assert
        resolved.Should().AllSatisfy(service => service.Should().NotBeNull());
        eventHandlers.Should().HaveCount(2);
        behaviors.Should().HaveCount(2);
        queryResult.IsSuccess.Should().BeTrue();
        queryResult.Data.Should().Be("Hello DI");
    }
}
