using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core;
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;
using SolTechnology.Core.Tale;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// Tests for the typed pause/cancel/version error markers and the engine behaviors.
/// </summary>
[TestFixture]
public class TypedErrorAndLifecycleTests
{
    private IServiceProvider _sp = null!;
    private InMemoryTaleRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        _repo = new InMemoryTaleRepository();
        var opts = new TaleOptions();
        // Provide a separate ITaleRepository singleton so the test can inspect it directly.
        services.AddSingleton<ITaleRepository>(_repo);
        services.AddSingleton(opts);
        services.AddScoped<TaleManager>();

        services.AddTransient<LifecyclePassingChapter>();
        services.AddTransient<LifecyclePauseChapter>();
        services.AddTransient<LifecycleCompleteChapter>();
        services.AddTransient<LifecycleTaleV1>();
        services.AddTransient<LifecycleTaleV2>();

        _sp = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        (_sp as IDisposable)?.Dispose();
    }

    private TaleManager Manager => _sp.GetRequiredService<TaleManager>();

    [Test]
    public async Task Pause_IsReportedAs_TalePausedError_NotStringMatch()
    {
        // Pure handler invocation should surface a TalePausedError, not a string-matched Fail.
        using var scope = _sp.CreateScope();
        var handler = ActivatorUtilities.CreateInstance<LifecycleTaleV1>(scope.ServiceProvider);
        var result = await handler.Handle(new LifecycleInput { Value = 1 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TalePausedError>();
        ((TalePausedError)result.Error!).ChapterId.Should().Be(nameof(LifecyclePauseChapter));
    }

    [Test]
    public async Task TaleManager_ShouldExpose_PausedTale_AsSuccess()
    {
        var start = await Manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });

        start.IsSuccess.Should().BeTrue();
        start.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
    }


    [Test]
    public async Task Cancel_PausedTale_ShouldSetStatusCancelled()
    {
        var start = await Manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        var storyId = start.Data!.TaleId;

        var cancel = await Manager.CancelStory(storyId);
        cancel.IsSuccess.Should().BeTrue();
        cancel.Data!.Status.Should().Be(TaleStatus.Cancelled);

        // Resume attempt should fail.
        var resume = await Manager.ResumeStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            storyId,
            JsonSerializer.SerializeToElement(new PausePayload { Token = "x" }));
        resume.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Cancel_CompletedTale_ShouldFail()
    {
        // Tale without interactive chapter finishes immediately.
        var start = await Manager.StartStory<LifecycleTaleV2, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        start.Data!.Status.Should().Be(TaleStatus.Completed);

        var cancel = await Manager.CancelStory(start.Data.TaleId);
        cancel.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task IdempotencyKey_ShouldDeduplicate_StartStory()
    {
        var first = await Manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 10 }, idempotencyKey: "idem-42");
        first.IsSuccess.Should().BeTrue();
        var firstId = first.Data!.TaleId;

        // Repeat with the same key — should return the same story.
        var second = await Manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 99 }, idempotencyKey: "idem-42");
        second.IsSuccess.Should().BeTrue();
        second.Data!.TaleId.Should().Be(firstId);
    }

    [Test]
    public async Task CreatedAt_ShouldBePreserved_AcrossSaves()
    {
        var start = await Manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });
        var originalCreatedAt = start.Data!.CreatedAt;

        await Task.Delay(50);

        // Force another save via resume (which will fail schema) — CreatedAt must not change.
        await Manager.ResumeStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            start.Data.TaleId,
            JsonSerializer.SerializeToElement(new PausePayload { Token = "ok" }));

        var latest = await _repo.FindById(start.Data.TaleId);
        latest!.CreatedAt.Should().Be(originalCreatedAt);
        latest.LastUpdatedAt.Should().BeAfter(originalCreatedAt);
    }

    [Test]
    public async Task ListAsync_ShouldReturn_MatchingStories()
    {
        await Manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 1 });
        await Manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 2 });
        await Manager.StartStory<LifecycleTaleV2, LifecycleInput, LifecycleContext, LifecycleOutput>(new LifecycleInput { Value = 3 });

        var paused = await _repo.ListAsync(status: TaleStatus.WaitingForInput);
        paused.Count.Should().Be(2);

        var completed = await _repo.ListAsync(status: TaleStatus.Completed);
        completed.Count.Should().Be(1);

        var v1Only = await _repo.ListAsync(handlerTypeName: nameof(LifecycleTaleV1));
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

public class LifecycleTaleV1 : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>
{
    public LifecycleTaleV1(IServiceProvider sp, ILogger<LifecycleTaleV1> log) : base(sp, log) { }
    protected override Tale<LifecycleOutput> Tell() =>
        Open<LifecyclePassingChapter>()
            .Read<LifecyclePauseChapter>()
            .Read<LifecycleCompleteChapter>()
            .Finale(ctx => ctx.Output);
}

public class LifecycleTaleV2 : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>
{
    public LifecycleTaleV2(IServiceProvider sp, ILogger<LifecycleTaleV2> log) : base(sp, log) { }
    protected override Tale<LifecycleOutput> Tell() =>
        Open<LifecyclePassingChapter>()
            .Read<LifecycleCompleteChapter>()
            .Finale(ctx => ctx.Output);
}

#endregion

