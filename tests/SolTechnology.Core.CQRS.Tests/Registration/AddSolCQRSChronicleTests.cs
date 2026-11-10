using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.CQRS.Tests.Registration;

[TestFixture]
public class AddSolCQRSChronicleTests
{
    [Test]
    public void AddSolCQRS_Default_RegistersChronicle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSolCQRS();

        // Assert
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetService<IChronicle>().Should().NotBeNull();
    }

    [Test]
    public void AddSolCQRS_UseLoggingFalse_DoesNotRegisterChronicle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSolCQRS(o => o.UseLogging = false);

        // Assert
        services.Should().NotContain(d => d.ServiceType == typeof(IChronicle));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IEventDispatcher>().Should().NotBeNull();
        provider.GetRequiredService<IEventPublisher>().Should().NotBeNull();
    }
}
