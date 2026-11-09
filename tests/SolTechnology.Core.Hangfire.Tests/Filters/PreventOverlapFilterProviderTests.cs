using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire.Tests.Filters;

[TestFixture]
public class PreventOverlapFilterProviderTests
{
    [Test]
    public void GetFilters_PreventOverlapTrue_ReturnsPreventOverlapFilter()
    {
        // Arrange
        var sut = new PreventOverlapFilterProvider([new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *", preventOverlap: true)]);

        // Act
        var filters = sut.GetFilters(RunnerJob<TestJob>());

        // Assert
        filters.Should().ContainSingle().Which.Instance.Should().BeOfType<PreventOverlapJobFilter>();
    }

    [Test]
    public void GetFilters_PreventOverlapFalse_ReturnsNoOverlapFilter()
    {
        // Arrange
        var sut = new PreventOverlapFilterProvider(
        [
            new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *"),
            new RecurringJobDescriptor(typeof(AnotherTestJob), "0 0 * * *", preventOverlap: true)
        ]);

        // Act
        var filters = sut.GetFilters(RunnerJob<TestJob>());

        // Assert
        filters.Should().BeEmpty();
    }

    [Test]
    public void GetFilters_NonRecurringJob_ReturnsNoOverlapFilter()
    {
        // Arrange
        var sut = new PreventOverlapFilterProvider([new RecurringJobDescriptor(typeof(TestJob), "0 0 * * *", preventOverlap: true)]);

        // Act
        var filters = sut.GetFilters(Job.FromExpression(() => Console.WriteLine("ad hoc")));

        // Assert
        filters.Should().BeEmpty();
    }

    [Test]
    public void UseSolFilters_PreventOverlapJob_ServerSeesOverlapFilter()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddHangfire((sp, configuration) => configuration.UseInMemoryStorage().UseSolFilters(sp));
        services.AddSolRecurringJob<TestJob>("0 0 * * *", preventOverlap: true);
        using var provider = services.BuildServiceProvider();
        var overlapProvider = provider.GetRequiredService<PreventOverlapFilterProvider>();

        try
        {
            // Act
            provider.GetRequiredService<IRecurringJobManager>();
            var serverFilters = provider.GetRequiredService<IJobFilterProvider>().GetFilters(RunnerJob<TestJob>());

            // Assert
            overlapProvider.IsInstalled.Should().BeTrue();
            serverFilters.Select(f => f.Instance).Should().ContainSingle(f => f is PreventOverlapJobFilter);
        }
        finally
        {
            HangfireGlobalState.Reset(provider);
        }
    }

    private static Job RunnerJob<TJob>() where TJob : class, IJob =>
        new(typeof(RecurringJobRunner<TJob>), typeof(RecurringJobRunner<TJob>).GetMethod(nameof(RecurringJobRunner<TJob>.RunAsync))!, CancellationToken.None);
}
