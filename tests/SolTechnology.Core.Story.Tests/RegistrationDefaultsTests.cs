using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core;
using SolTechnology.Core.Story.Builder;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Dense scenario tests for the Story framework registration surface. Each test drives a
/// realistic end-to-end slice rather than asserting trivial container state — keeping the
/// suite signal-heavy per the project's testing philosophy.
/// </summary>
[TestFixture]
public class RegistrationDefaultsTests
{
    /// <summary>
    /// Plain <c>RegisterStories()</c> must wire InMemory persistence, scoped StoryManager
    /// and support a full pause → resume → complete cycle out of the box.
    /// </summary>
    [Test]
    public async Task RegisterStories_Defaults_WireInMemory_AndSupportFullPauseResumeCycle()
    {
        using var sp = BuildProvider(b => { /* defaults */ });

        sp.GetRequiredService<IStoryRepository>().Should().BeOfType<InMemoryStoryRepository>();
        sp.GetRequiredService<StoryManager>().Should().NotBeNull();
        sp.GetRequiredService<StoryHandlerRegistry>().Should().NotBeNull();

        await AssertFullCycleCompletes(sp);
    }


    /// <summary>
    /// <c>.UseStoryRepository&lt;T&gt;()</c> is the OCP escape hatch. A custom repository
    /// type must replace the default, be resolved with the caller-chosen lifetime, and
    /// actually receive persistence calls from the engine.
    /// </summary>
    [Test]
    public async Task UseStoryRepository_Generic_ReplacesDefault_WithRequestedLifetime_AndIsUsedByEngine()
    {
        var services = new ServiceCollection().AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.RegisterStories(assemblies: typeof(LifecycleStoryV1).Assembly)
                .UseStoryRepository<RecordingStoryRepository>(ServiceLifetime.Singleton);

        using var sp = services.BuildServiceProvider();

        var resolved = sp.GetRequiredService<IStoryRepository>();
        resolved.Should().BeOfType<RecordingStoryRepository>();

        var manager = sp.GetRequiredService<StoryManager>();
        var start = await manager.StartStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });

        start.IsSuccess.Should().BeTrue();
        ((RecordingStoryRepository)resolved).SaveCount
            .Should().BeGreaterThan(0, "the engine must write at least one state transition through the registered custom repository");

        // Singleton lifetime: same instance across multiple resolves.
        sp.GetRequiredService<IStoryRepository>().Should().BeSameAs(resolved);
    }


    // --- helpers ---

    private static ServiceProvider BuildProvider(Action<IStoryBuilder> configureBuilder)
    {
        var services = new ServiceCollection().AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        var builder = services.RegisterStories(assemblies: typeof(LifecycleStoryV1).Assembly);
        configureBuilder(builder);
        return services.BuildServiceProvider();
    }

    private static async Task AssertFullCycleCompletes(IServiceProvider sp)
    {
        var manager = sp.GetRequiredService<StoryManager>();

        var start = await manager.StartStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        start.IsSuccess.Should().BeTrue();
        start.Data!.Status.Should().Be(StoryStatus.WaitingForInput);

        var resumeInput = JsonSerializer.SerializeToElement(new PausePayload { Token = "abc" });
        var resumed = await manager.ResumeStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            start.Data.StoryId, resumeInput);
        resumed.IsSuccess.Should().BeTrue();
        resumed.Data!.Status.Should().Be(StoryStatus.Completed);
    }


    private sealed class RecordingStoryRepository : IStoryRepository
    {
        private readonly InMemoryStoryRepository _inner = new();
        public int SaveCount;
        public int FindByIdCount;

        public Task<StoryInstance?> FindById(Auid storyId)
        {
            Interlocked.Increment(ref FindByIdCount);
            return _inner.FindById(storyId);
        }

        public Task<StoryInstance?> FindByIdempotencyKey(string idempotencyKey)
            => _inner.FindByIdempotencyKey(idempotencyKey);

        public Task<IReadOnlyList<StoryInstance>> ListAsync(
            StoryStatus? status = null,
            string? handlerTypeName = null,
            int skip = 0,
            int take = 100)
            => _inner.ListAsync(status, handlerTypeName, skip, take);

        public Task SaveAsync(StoryInstance storyInstance)
        {
            Interlocked.Increment(ref SaveCount);
            return _inner.SaveAsync(storyInstance);
        }

        public Task DeleteAsync(Auid storyId) => _inner.DeleteAsync(storyId);
    }
}
