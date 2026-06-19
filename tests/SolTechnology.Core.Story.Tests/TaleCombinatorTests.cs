using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Tale;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Direct coverage for the Tale combinators: Expect, Otherwise, WhenLost, WhenWon, Do and Finale.
/// </summary>
[TestFixture]
public class TaleCombinatorTests
{
    // Disposed in TearDown below; NUnit1032 does not recognise the disposal through the helper.
#pragma warning disable NUnit1032
    private IServiceProvider _sp = null!;
#pragma warning restore NUnit1032

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton(new StoryOptions());
        services.AddTransient<TalePassChapter>();
        services.AddTransient<TaleBonusChapter>();
        services.AddTransient<TaleFailChapter>();
        services.AddTransient<TaleRecoverChapter>();
        _sp = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        if (_sp is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
        }
        else if (_sp is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private T Build<T>() where T : class
        => ActivatorUtilities.CreateInstance<T>(_sp);

    [Test]
    public async Task Expect_WhenConditionHolds_StoryContinues()
    {
        var result = await Build<ExpectPassStory>().Handle(new TaleInput());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Value.Should().Be(11); // Pass (1) then bonus chapter (+10)
    }

    [Test]
    public async Task Expect_WhenConditionFails_StoryStopsWithThatError()
    {
        var handler = Build<ExpectFailStory>();

        var result = await handler.Handle(new TaleInput());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error!.Message.Should().Be("too small");
        handler.Context.Value.Should().Be(1); // second Pass chapter never ran
    }

    [Test]
    public async Task Otherwise_Chapter_RecoversFromFailure()
    {
        var result = await Build<OtherwiseChapterStory>().Handle(new TaleInput());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Value.Should().Be(99); // recover chapter put it back on track
    }

    [Test]
    public async Task Otherwise_Lambda_RecoversFromFailure()
    {
        var result = await Build<OtherwiseLambdaStory>().Handle(new TaleInput());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Value.Should().Be(42);
    }

    [Test]
    public async Task Otherwise_OnSuccess_IsIgnored()
    {
        var result = await Build<OtherwiseIgnoredStory>().Handle(new TaleInput());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Value.Should().Be(1); // recover chapter did NOT run
    }

    [Test]
    public async Task WhenLost_FiresOnlyOnFailure()
    {
        var handler = Build<WhenLostStory>();

        var result = await handler.Handle(new TaleInput());

        result.IsFailure.Should().BeTrue();
        handler.Context.Log.Should().Contain("lost:boom");
    }

    [Test]
    public async Task WhenWon_FiresOnlyOnSuccess()
    {
        var handler = Build<WhenWonStory>();

        var result = await handler.Handle(new TaleInput());

        result.IsSuccess.Should().BeTrue();
        handler.Context.Log.Should().Contain("won");
    }

    [Test]
    public async Task Do_InlineFailure_ShortCircuitsRemainingChapters()
    {
        var handler = Build<DoFailStory>();

        var result = await handler.Handle(new TaleInput());

        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Be("inline boom");
        handler.Context.Value.Should().Be(1); // chapter after the failing Do never ran
    }

    [Test]
    public async Task Finale_ProjectsTheContextIntoOutput()
    {
        var result = await Build<FinaleStory>().Handle(new TaleInput());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Value.Should().Be(7); // projected from ctx.Value via Finale
    }
}

#region Fixtures

public class TaleInput { public int Seed { get; set; } }

public class TaleOutput { public int Value { get; set; } }

public class TaleTestContext : Context<TaleInput, TaleOutput>
{
    public int Value { get; set; }
    public List<string> Log { get; } = new();
}

public class TalePassChapter : Chapter<TaleTestContext>
{
    public override Task<Result> Read(TaleTestContext ctx)
    {
        ctx.Value += 1;
        ctx.Log.Add("pass");
        return Result.SuccessAsTask();
    }
}

public class TaleBonusChapter : Chapter<TaleTestContext>
{
    public override Task<Result> Read(TaleTestContext ctx)
    {
        ctx.Value += 10;
        ctx.Log.Add("bonus");
        return Result.SuccessAsTask();
    }
}

public class TaleFailChapter : Chapter<TaleTestContext>
{
    public override Task<Result> Read(TaleTestContext ctx)
    {
        ctx.Log.Add("fail");
        return Result.FailAsTask("boom");
    }
}

public class TaleRecoverChapter : Chapter<TaleTestContext>
{
    public override Task<Result> Read(TaleTestContext ctx)
    {
        ctx.Value = 99;
        ctx.Log.Add("recover");
        return Result.SuccessAsTask();
    }
}

public class ExpectPassStory(IServiceProvider sp, ILogger<ExpectPassStory> log)
    : StoryHandler<TaleInput, TaleTestContext, TaleOutput>(sp, log)
{
    protected override Tale<TaleOutput> Tell() =>
        Open<TalePassChapter>()
            .Expect(ctx => ctx.Value >= 1, new NotFoundError { Message = "too small" })
            .Read<TaleBonusChapter>()
            .Finale(ctx => new TaleOutput { Value = ctx.Value });
}

public class ExpectFailStory(IServiceProvider sp, ILogger<ExpectFailStory> log)
    : StoryHandler<TaleInput, TaleTestContext, TaleOutput>(sp, log)
{
    protected override Tale<TaleOutput> Tell() =>
        Open<TalePassChapter>()
            .Expect(ctx => ctx.Value > 100, new NotFoundError { Message = "too small" })
            .Read<TalePassChapter>()
            .Finale(ctx => new TaleOutput { Value = ctx.Value });
}

public class OtherwiseChapterStory(IServiceProvider sp, ILogger<OtherwiseChapterStory> log)
    : StoryHandler<TaleInput, TaleTestContext, TaleOutput>(sp, log)
{
    protected override Tale<TaleOutput> Tell() =>
        Open<TaleFailChapter>()
            .Otherwise<TaleRecoverChapter>()
            .Finale(ctx => new TaleOutput { Value = ctx.Value });
}

public class OtherwiseLambdaStory(IServiceProvider sp, ILogger<OtherwiseLambdaStory> log)
    : StoryHandler<TaleInput, TaleTestContext, TaleOutput>(sp, log)
{
    protected override Tale<TaleOutput> Tell() =>
        Open<TaleFailChapter>()
            .Otherwise(ctx => { ctx.Value = 42; return Result.SuccessAsTask(); })
            .Finale(ctx => new TaleOutput { Value = ctx.Value });
}

public class OtherwiseIgnoredStory(IServiceProvider sp, ILogger<OtherwiseIgnoredStory> log)
    : StoryHandler<TaleInput, TaleTestContext, TaleOutput>(sp, log)
{
    protected override Tale<TaleOutput> Tell() =>
        Open<TalePassChapter>()
            .Otherwise<TaleRecoverChapter>()
            .Finale(ctx => new TaleOutput { Value = ctx.Value });
}

public class WhenLostStory(IServiceProvider sp, ILogger<WhenLostStory> log)
    : StoryHandler<TaleInput, TaleTestContext, TaleOutput>(sp, log)
{
    protected override Tale<TaleOutput> Tell() =>
        Open<TaleFailChapter>()
            .WhenLost(error => Context.Log.Add($"lost:{error.Message}"))
            .Finale(ctx => new TaleOutput { Value = ctx.Value });
}

public class WhenWonStory(IServiceProvider sp, ILogger<WhenWonStory> log)
    : StoryHandler<TaleInput, TaleTestContext, TaleOutput>(sp, log)
{
    protected override Tale<TaleOutput> Tell() =>
        Open<TalePassChapter>()
            .WhenWon(ctx => ctx.Log.Add("won"))
            .Finale(ctx => new TaleOutput { Value = ctx.Value });
}

public class DoFailStory(IServiceProvider sp, ILogger<DoFailStory> log)
    : StoryHandler<TaleInput, TaleTestContext, TaleOutput>(sp, log)
{
    protected override Tale<TaleOutput> Tell() =>
        Open<TalePassChapter>()
            .Do(_ => Result.FailAsTask("inline boom"))
            .Read<TalePassChapter>()
            .Finale(ctx => new TaleOutput { Value = ctx.Value });
}

public class FinaleStory(IServiceProvider sp, ILogger<FinaleStory> log)
    : StoryHandler<TaleInput, TaleTestContext, TaleOutput>(sp, log)
{
    protected override Tale<TaleOutput> Tell() =>
        Open<TalePassChapter>()
            .Do(ctx => ctx.Value = 7)
            .Finale(ctx => new TaleOutput { Value = ctx.Value });
}

#endregion


