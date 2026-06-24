using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Testing.Substitutes;

namespace SolTechnology.Core.Hangfire.Tests;

[TestFixture]
public class RecurringJobTests
{
    [Test]
    public void AddRecurringJob_RegistersJobAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRecurringJob<TestJob>("0 0 * * *");
        var sp = services.BuildServiceProvider();

        // Assert
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<TestJob>().Should().NotBeNull();
    }

    [Test]
    public void AddRecurringJob_RegistersRecurringJobRegistrarAsHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRecurringJobManager>());

        // Act
        services.AddRecurringJob<TestJob>("0 0 * * *");
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetServices<IHostedService>()
            .Should().ContainSingle(s => s is RecurringJobRegistrar);
    }

    [Test]
    public void AddRecurringJob_CalledTwice_RegistersOnlyOneRegistrar()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IRecurringJobManager>());

        // Act
        services.AddRecurringJob<TestJob>("0 0 * * *");
        services.AddRecurringJob<AnotherTestJob>("0 12 * * *");
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetServices<IHostedService>()
            .OfType<RecurringJobRegistrar>()
            .Should().HaveCount(1);
    }

    [Test]
    public void AddRecurringJob_NullCronExpression_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddRecurringJob<TestJob>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task RecurringJobRegistrar_StartAsync_RegistersJobWithManager()
    {
        // Arrange
        var manager = Substitute.For<IRecurringJobManager>();
        var descriptors = new[]
        {
            new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *")
        };
        var registrar = new RecurringJobRegistrar(manager, descriptors);

        // Act
        await registrar.StartAsync(CancellationToken.None);

        // Assert
        manager.Received(1).AddOrUpdate(
            "TestJob",
            Arg.Any<Job>(),
            "0 0 * * *",
            Arg.Any<RecurringJobOptions>());
    }

    [Test]
    public async Task RecurringJobRegistrar_MultipleDescriptors_RegistersAll()
    {
        // Arrange
        var manager = Substitute.For<IRecurringJobManager>();
        var descriptors = new[]
        {
            new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *"),
            new RecurringJobDescriptor(typeof(AnotherTestJob), "0 12 * * *")
        };
        var registrar = new RecurringJobRegistrar(manager, descriptors);

        // Act
        await registrar.StartAsync(CancellationToken.None);

        // Assert
        manager.Received(2).AddOrUpdate(
            Arg.Any<string>(),
            Arg.Any<Job>(),
            Arg.Any<string>(),
            Arg.Any<RecurringJobOptions>());
    }

    [Test]
    public async Task RecurringJobRegistrar_UsesJobTypeNameAsId()
    {
        // Arrange
        var manager = Substitute.For<IRecurringJobManager>();
        var descriptors = new[]
        {
            new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *")
        };
        var registrar = new RecurringJobRegistrar(manager, descriptors);

        // Act
        await registrar.StartAsync(CancellationToken.None);

        // Assert
        manager.Received(1).AddOrUpdate(
            "TestJob",
            Arg.Any<Job>(),
            Arg.Any<string>(),
            Arg.Any<RecurringJobOptions>());
    }

    [Test]
    public async Task RecurringJobRunner_ResolvesJobFromScopeAndExecutes()
    {
        // Arrange
        var job = Substitute.For<TestJob>();
        var services = new ServiceCollection();
        services.AddScoped(_ => job);
        var sp = services.BuildServiceProvider();
        var runner = new RecurringJobRunner<TestJob>(sp.GetRequiredService<IServiceScopeFactory>());

        // Act
        await runner.RunAsync(CancellationToken.None);

        // Assert
        await job.Received(1).Execute(Ct.Any);
    }
}

public class TestJob : IJob
{
    public virtual Task Execute(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class AnotherTestJob : IJob
{
    public virtual Task Execute(CancellationToken cancellationToken) => Task.CompletedTask;
}



