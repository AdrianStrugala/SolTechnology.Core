using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Builder;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// Dense scenario tests for the Tale framework registration surface. Each test drives a
/// realistic end-to-end slice rather than asserting trivial container state — keeping the
/// suite signal-heavy per the project's testing philosophy.
/// </summary>
[TestFixture]
public class RegistrationDefaultsTests
{
    /// <summary>
    /// Plain <c>AddSolTale()</c> must wire InMemory persistence, scoped TaleManager
    /// and support a full pause → resume → complete cycle out of the box.
    /// </summary>
    [Test]
    public async Task AddSolTale_Defaults_WireInMemory_AndSupportFullPauseResumeCycle()
    {
        using var sp = BuildProvider(b => { /* defaults */ });

        sp.GetRequiredService<ITaleRepository>().Should().BeOfType<InMemoryTaleRepository>();
        sp.GetRequiredService<TaleManager>().Should().NotBeNull();
        sp.GetRequiredService<TaleHandlerRegistry>().Should().NotBeNull();

        await AssertFullCycleCompletes(sp);
    }


    /// <summary>
    /// <c>.UseTaleRepository&lt;T&gt;()</c> is the OCP escape hatch. A custom repository
    /// type must replace the default, be resolved with the caller-chosen lifetime, and
    /// actually receive persistence calls from the engine.
    /// </summary>
    [Test]
    public async Task UseTaleRepository_Generic_ReplacesDefault_WithRequestedLifetime_AndIsUsedByEngine()
    {
        var services = new ServiceCollection().AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSolTale(assemblies: typeof(LifecycleTaleV1).Assembly)
                .UseTaleRepository<RecordingTaleRepository>(ServiceLifetime.Singleton);

        using var sp = services.BuildServiceProvider();

        var resolved = sp.GetRequiredService<ITaleRepository>();
        resolved.Should().BeOfType<RecordingTaleRepository>();

        var manager = sp.GetRequiredService<TaleManager>();
        var start = await manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });

        start.IsSuccess.Should().BeTrue();
        ((RecordingTaleRepository)resolved).SaveCount
            .Should().BeGreaterThan(0, "the engine must write at least one state transition through the registered custom repository");

        // Singleton lifetime: same instance across multiple resolves.
        sp.GetRequiredService<ITaleRepository>().Should().BeSameAs(resolved);
    }


    [Test]
    public void AddSolTale_NoArgs_ScansCallerAssembly()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();

        // Act
        services.AddSolTale();

        // Assert
        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<TaleHandlerRegistry>().TryResolve(nameof(LifecycleTaleV1), out var handlerType).Should().BeTrue();
        handlerType.Should().Be(typeof(LifecycleTaleV1));
        services.Should().Contain(d => d.ServiceType == typeof(LifecyclePauseChapter));
    }

    [Test]
    public async Task Handle_NewTale_LogScopeHasAssignedTaleId()
    {
        // Arrange
        var logs = new RecordingLoggerProvider();
        var services = new ServiceCollection().AddLogging(b => b.AddProvider(logs));
        services.AddSolTale(assemblies: typeof(LifecycleTaleV1).Assembly);
        using var sp = services.BuildServiceProvider();
        var manager = sp.GetRequiredService<TaleManager>();

        // Act
        var start = await manager.StartStory<LifecycleTaleV2, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });

        // Assert
        var taleScope = logs.Scopes.OfType<IReadOnlyDictionary<string, object?>>()
            .Should().ContainSingle(scope => scope.ContainsKey("TaleId")).Subject;
        taleScope["TaleId"].Should().Be(start.Data!.TaleId).And.NotBe(Auid.Empty);
        taleScope["TaleHandler"].Should().Be(nameof(LifecycleTaleV2));
    }

    // --- helpers ---

    private static ServiceProvider BuildProvider(Action<ITaleBuilder> configureBuilder)
    {
        var services = new ServiceCollection().AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        var builder = services.AddSolTale(assemblies: typeof(LifecycleTaleV1).Assembly);
        configureBuilder(builder);
        return services.BuildServiceProvider();
    }

    private static async Task AssertFullCycleCompletes(IServiceProvider sp)
    {
        var manager = sp.GetRequiredService<TaleManager>();

        var start = await manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        start.IsSuccess.Should().BeTrue();
        start.Data!.Status.Should().Be(TaleStatus.WaitingForInput);

        var resumeInput = JsonSerializer.SerializeToElement(new PausePayload { Token = "abc" });
        var resumed = await manager.ResumeStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            start.Data.TaleId, resumeInput);
        resumed.IsSuccess.Should().BeTrue();
        resumed.Data!.Status.Should().Be(TaleStatus.Completed);
    }


    private sealed class RecordingTaleRepository : ITaleRepository
    {
        private readonly InMemoryTaleRepository _inner = new();
        public int SaveCount;
        public int FindByIdCount;

        public Task<TaleInstance?> FindById(Auid taleId, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref FindByIdCount);
            return _inner.FindById(taleId, cancellationToken);
        }

        public Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken)
            => _inner.FindByIdempotencyKey(idempotencyKey, cancellationToken);

        public Task<IReadOnlyList<TaleInstance>> List(
            TaleStatus? status = null,
            string? handlerTypeName = null,
            int skip = 0,
            int take = 100,
            CancellationToken cancellationToken = default)
            => _inner.List(status, handlerTypeName, skip, take, cancellationToken);

        public Task<Result> Save(TaleInstance taleInstance, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref SaveCount);
            return _inner.Save(taleInstance, cancellationToken);
        }

        public Task Delete(Auid taleId, CancellationToken cancellationToken) => _inner.Delete(taleId, cancellationToken);
    }
}
