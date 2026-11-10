using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleModuleInstallerTests
{
    [Test]
    public void AddSolChronicle_CalledTwice_RegistersOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSolChronicle().AddSolChronicle(o => o.MaxSteps = 10);

        // Assert
        services.Count(d => d.ServiceType == typeof(IChronicle)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(IChronicleRootListener)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(IValidateOptions<ChronicleOptions>)).Should().Be(1);
    }

    [Test]
    public async Task AddSolChronicle_InvalidOptions_FailsOnStart()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureServices(services => services.AddSolChronicle(o => o.MaxSteps = 0))
            .Build();

        // Act
        var act = () => host.StartAsync();

        // Assert
        (await act.Should().ThrowAsync<OptionsValidationException>()).Which.Message.Should().Contain("MaxSteps");
    }

    [Test]
    public void AddSolLoggingConfiguration_BindsChronicleSection()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();

        // Act
        services.AddSolLogging(Configuration(("SampleRate", "0.25")));

        // Assert
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOptions<ChronicleOptions>>().Value.SampleRate.Should().Be(0.25);
        provider.GetRequiredService<IChronicle>().Should().BeOfType<ChronicleRecorder>();
    }

    [Test]
    public void AddSolLoggingAction_RegistersChronicle()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();

        // Act
        services.AddSolLogging(o => o.SkipPaths = ["/health"]);

        // Assert
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IChronicle>().Should().BeOfType<ChronicleRecorder>();
        provider.GetServices<IChronicleRootListener>().Should().ContainSingle().Which.Should().BeOfType<ChronicleReporter>();
    }

    [Test]
    public void AddSolChronicle_ParameterlessThenConfiguration_BindsSection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSolChronicle().AddSolChronicle(Configuration(("Enabled", "false")));

        // Assert
        Resolve(services).Enabled.Should().BeFalse();
    }

    [Test]
    public void AddSolChronicle_ConfigurationThenParameterless_KeepsSection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSolChronicle(Configuration(("Enabled", "false"))).AddSolChronicle();

        // Assert
        Resolve(services).Enabled.Should().BeFalse();
    }

    [Test]
    public void AddSolChronicle_ActionAfterInstallerCall_Applies()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolLogging(Configuration(("MaxSteps", "100")));

        // Act
        services.AddSolChronicle(o => o.MaxDepth = 4);

        // Assert
        var options = Resolve(services);
        options.MaxSteps.Should().Be(100);
        options.MaxDepth.Should().Be(4);
    }

    private static ChronicleOptions Resolve(IServiceCollection services)
        => services.BuildServiceProvider().GetRequiredService<IOptions<ChronicleOptions>>().Value;

    private static IConfiguration Configuration(params (string Key, string Value)[] values) => new ConfigurationBuilder()
        .AddInMemoryCollection(values.ToDictionary(v => $"{ChronicleOptions.SectionName}:{v.Key}", v => (string?)v.Value))
        .Build();
}
