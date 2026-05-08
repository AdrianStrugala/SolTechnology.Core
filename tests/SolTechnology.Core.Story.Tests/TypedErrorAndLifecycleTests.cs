using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Tests for the typed pause/cancel/version error markers and the engine behaviors.
/// </summary>
[TestFixture]
public class TypedErrorAndLifecycleTests
{
    private IServiceProvider _sp = null!;
    private InMemoryStoryRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        _repo = new InMemoryStoryRepository();
        var opts = new StoryOptions();
        // Provide a separate IStoryRepository singleton so the test can inspect it directly.
        services.AddSingleton<IStoryRepository>(_repo);
        services.AddSingleton(opts);
        services.AddScoped<StoryManager>();

        services.AddTransient<LifecyclePassingChapter>();
        services.AddTransient<LifecyclePauseChapter>();
        services.AddTransient<LifecycleCompleteChapter>();
        services.AddTransient<LifecycleStoryV1>();
        services.AddTransient<LifecycleStoryV2>();

        _sp = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        (_sp as IDisposable)?.Dispose();
    }

    private StoryManager Manager => _sp.GetRequiredService<StoryManager>();

    [Test]
    public async Task Pause_IsReportedAs_StoryPausedError_NotStringMatch()
    {
        // Pure handler invocation should surface a StoryPausedError, not a string-matched Fail.
        using var scope = _sp.CreateScope();
        var handler = ActivatorUtilities.CreateInstance<LifecycleStoryV1>(scope.ServiceProvider);
        var result = await handler.Handle(new LifecycleInput { Value = 1 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<StoryPausedError>();
        ((StoryPausedError)result.Error!).ChapterId.Should().Be(nameof(LifecyclePauseChapter));
    }

    [Test]
    public async Task StoryManager_ShouldExpose_PausedStory_AsSuccess()
    {
        var start = await Manager.StartStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });

        start.IsSuccess.Should().BeTrue();
        start.Data!.Status.Should().Be(StoryStatus.WaitingForInput);
    }


    [Test]
    public async Task Cancel_PausedStory_ShouldSetStatusCancelled()
    {
        var start = await Manager.StartStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        var storyId = start.Data!.StoryId;

        var cancel = await Manager.CancelStory(storyId);
        cancel.IsSuccess.Should().BeTrue();
        cancel.Data!.Status.Should().Be(StoryStatus.Cancelled);

        // Resume attempt should fail.
        var resume = await Manager.ResumeStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            storyId,
            JsonSerializer.SerializeToElement(new PausePayload { Token = "x" }));
        resume.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Cancel_CompletedStory_ShouldFail()
    {
        // Story without interactive chapter finishes immediately.
        var start = await Manager.StartStory<LifecycleStoryV2, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        start.Data!.Status.Should().Be(StoryStatus.Completed);

        var cancel = await Manager.CancelStory(start.Data.StoryId);
        cancel.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task IdempotencyKey_ShouldDeduplicate_StartStory()
    {
        var first = await Manager.StartStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 10 }, idempotencyKey: "idem-42");
        first.IsSuccess.Should().BeTrue();
        var firstId = first.Data!.StoryId;

        // Repeat with the same key — should return the same story.
        var second = await Manager.StartStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 99 }, idempotencyKey: "idem-42");
        second.IsSuccess.Should().BeTrue();
        second.Data!.StoryId.Should().Be(firstId);
    }

    [Test]
    public async Task CreatedAt_ShouldBePreserved_AcrossSaves()
    {
        var start = await Manager.StartStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        var originalCreatedAt = start.Data!.CreatedAt;

        await Task.Delay(50);

        // Force another save via resume (which will fail schema) — CreatedAt must not change.
        await Manager.ResumeStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            start.Data.StoryId,
            JsonSerializer.SerializeToElement(new PausePayload { Token = "ok" }));

        var latest = await _repo.FindById(start.Data.StoryId);
        latest!.CreatedAt.Should().Be(originalCreatedAt);
        latest.LastUpdatedAt.Should().BeAfter(originalCreatedAt);
    }

    [Test]
    public async Task ListAsync_ShouldReturn_MatchingStories()
    {
        await Manager.StartStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });
        await Manager.StartStory<LifecycleStoryV1, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 2 });
        await Manager.StartStory<LifecycleStoryV2, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 3 });

        var paused = await _repo.ListAsync(status: StoryStatus.WaitingForInput);
        paused.Count.Should().Be(2);

        var completed = await _repo.ListAsync(status: StoryStatus.Completed);
        completed.Count.Should().Be(1);

        var v1Only = await _repo.ListAsync(handlerTypeName: nameof(LifecycleStoryV1));
        v1Only.Count.Should().Be(2);
    }
}

#region Support types

public class LifecycleInput { public int Value { get; set; } }
public class LifecycleOutput { public string Message { get; set; } = ""; }

public class LifecycleContext : Context<LifecycleInput, LifecycleOutput>
{
    public string? Token { get; set; }
}

public class PausePayload { public string Token { get; set; } = ""; }

public class LifecyclePassingChapter : Chapter<LifecycleContext>
{
    public override Task<Result> Read(LifecycleContext ctx) => Result.SuccessAsTask();
}

public class LifecyclePauseChapter : InteractiveChapter<LifecycleContext, PausePayload>
{
    public override Task<Result> ReadWithInput(LifecycleContext ctx, PausePayload input)
    {
        ctx.Token = input.Token;
        return Result.SuccessAsTask();
    }
}

public class LifecycleCompleteChapter : Chapter<LifecycleContext>
{
    public override Task<Result> Read(LifecycleContext ctx)
    {
        ctx.Output.Message = "done";
        return Result.SuccessAsTask();
    }
}

public class LifecycleStoryV1 : StoryHandler<LifecycleInput, LifecycleContext, LifecycleOutput>
{
    public LifecycleStoryV1(IServiceProvider sp, ILogger<LifecycleStoryV1> log) : base(sp, log) { }
    protected override async Task TellStory()
    {
        await ReadChapter<LifecyclePassingChapter>();
        await ReadChapter<LifecyclePauseChapter>();
        await ReadChapter<LifecycleCompleteChapter>();
    }
}

public class LifecycleStoryV2 : StoryHandler<LifecycleInput, LifecycleContext, LifecycleOutput>
{
    public LifecycleStoryV2(IServiceProvider sp, ILogger<LifecycleStoryV2> log) : base(sp, log) { }
    protected override async Task TellStory()
    {
        await ReadChapter<LifecyclePassingChapter>();
        await ReadChapter<LifecycleCompleteChapter>();
    }
}

#endregion

