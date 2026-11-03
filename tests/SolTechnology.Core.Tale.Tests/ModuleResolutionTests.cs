using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

[TestFixture]
public class ModuleResolutionTests
{
    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Test]
    public async Task AddSolTale_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<TestService>();
        services.AddSolTale(assemblies: typeof(ModuleResolutionTests).Assembly);
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var resolved = new object[]
        {
            scoped.GetRequiredService<TaleManager>(),
            scoped.GetRequiredService<ITaleRepository>(),
            scoped.GetRequiredService<TaleHandlerRegistry>(),
            scoped.GetRequiredService<TaleOptions>(),
            scoped.GetRequiredService<LifecycleTaleV2>(),
            scoped.GetRequiredService<LifecyclePassingChapter>()
        };
        var start = await scoped.GetRequiredService<TaleManager>()
            .StartStory<LifecycleTaleV2, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });

        // Assert
        resolved.Should().AllSatisfy(service => service.Should().NotBeNull());
        start.IsSuccess.Should().BeTrue();
        start.Data!.Status.Should().Be(TaleStatus.Completed);
    }
}
