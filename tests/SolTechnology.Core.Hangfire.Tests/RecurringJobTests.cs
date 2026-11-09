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
    public void AddSolRecurringJob_RegistersJobAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSolRecurringJob<TestJob>("0 0 * * *");
        var sp = services.BuildServiceProvider();

        // Assert
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<TestJob>().Should().NotBeNull();
    }

    [Test]
    public void AddSolRecurringJob_RegistersRecurringJobRegistrarAsHostedService()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(Substitute.For<IRecurringJobManager>());

        // Act
        services.AddSolRecurringJob<TestJob>("0 0 * * *");
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetServices<IHostedService>()
            .Should().ContainSingle(s => s is RecurringJobRegistrar);
    }

    [Test]
    public void AddSolRecurringJob_CalledTwice_RegistersOnlyOneRegistrar()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(Substitute.For<IRecurringJobManager>());

        // Act
        services.AddSolRecurringJob<TestJob>("0 0 * * *");
        services.AddSolRecurringJob<AnotherTestJob>("0 12 * * *");
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetServices<IHostedService>()
            .OfType<RecurringJobRegistrar>()
            .Should().HaveCount(1);
    }

    [Test]
    public void AddSolRecurringJob_NullCronExpression_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddSolRecurringJob<TestJob>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
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



