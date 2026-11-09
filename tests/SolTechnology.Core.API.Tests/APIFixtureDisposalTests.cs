using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using SolTechnology.Core.API.Testing;

namespace SolTechnology.Core.API.Tests;

[TestFixture]
public class APIFixtureDisposalTests
{
    [Test]
    public void Fixture_Dispose_StopsHostedServices()
    {
        // Arrange
        var probe = new StopProbe();
        var fixture = new APIFixture<DisposalTestEntryPoint>(
            configureServices: services => services.AddSingleton<IHostedService>(probe));

        // Act
        fixture.Dispose();

        // Assert
        probe.IsStarted.Should().BeTrue();
        probe.IsStopped.Should().BeTrue();
        fixture.Invoking(f => f.Dispose()).Should().NotThrow();
    }

    private sealed class StopProbe : IHostedService
    {
        public bool IsStarted { get; private set; }

        public bool IsStopped { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            IsStarted = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            IsStopped = true;
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Minimal web host for <see cref="APIFixtureDisposalTests"/>: <c>WebApplicationFactory</c> boots the
/// entry point of the assembly that holds its type argument, so this project replaces the generated test entry point.
/// </summary>
public sealed class DisposalTestEntryPoint
{
    public static void Main(string[] args)
    {
        // The factory's solution-relative content root does not exist for a test project; use the output folder.
        var app = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        }).Build();
        app.Run();
    }
}
