using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// Tests for TaleEngine internal orchestration.
/// Verifies error aggregation, chapter execution flow, and engine state management.
/// </summary>
[TestFixture]
public class TaleEngineTests
{
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Register test chapters
        services.AddTransient<EngineTestChapter1>();
        services.AddTransient<EngineTestChapter2>();
        services.AddTransient<EngineTestChapter3>();
        services.AddTransient<EngineTestFailingChapter>();
        services.AddTransient<EngineTestFailingChapter2>();
        services.AddTransient<EngineTestFailingChapter3>();
        services.AddTransient<EngineTestSingleErrorChapter>();
        services.AddTransient<EngineTestThrowingChapter>();
        services.AddTransient<EngineTestBlockingChapter>();
        services.AddTransient<Inbound.RecordStepChapter>();
        services.AddTransient<Outbound.RecordStepChapter>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Test]
    public async Task TaleEngine_ShouldExecuteAllChapters_InSequence()
    {
        // Arrange
        var handler = new EngineTestTale(_serviceProvider, GetLogger<EngineTestTale>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.Context.ExecutionOrder.Should().Equal(new[] { "Chapter1", "Chapter2", "Chapter3" });
    }

    [Test]
    public async Task TaleEngine_ShouldStopOnFirstError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(new TaleOptions());
        services.AddTransient<EngineTestFailingChapter>();
        services.AddTransient<EngineTestFailingChapter2>();
        services.AddTransient<EngineTestFailingChapter3>();
        var sp = services.BuildServiceProvider();

        var handler = new MultipleErrorsTale(sp, sp.GetRequiredService<ILogger<MultipleErrorsTale>>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeOfType<AggregateError>(); // single, first error
        result.Error!.Message.Should().Be("Error from Chapter 1");

        // Only the first chapter should have executed
        handler.Context.ExecutionOrder.Should().Equal(new[] { "Chapter1" });
    }


    [Test]
    public async Task TaleEngine_ShouldConvertExceptions_ToErrors()
    {
        // Arrange
        var handler = new ThrowingTale(_serviceProvider, GetLogger<ThrowingTale>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("Intentional exception");
    }

    [Test]
    public async Task TaleEngine_ShouldHandleCancellation()
    {
        // Arrange
        var handler = new LongRunningTale(_serviceProvider, GetLogger<LongRunningTale>());
        var input = new EngineTestInput { Value = 1 };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await handler.Handle(input, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("cancelled");
    }

    [Test]
    public async Task Chapter_ReceivesRunToken_AndCancellationStopsInFlightWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var handler = new BlockingTale(_serviceProvider, GetLogger<BlockingTale>());
        var run = handler.Handle(new EngineTestInput { Value = 1 }, cts.Token);

        // Act
        cts.Cancel();
        var result = await run;

        // Assert
        handler.Context.Received.Should().Be(cts.Token);
        handler.Context.Work!.IsCanceled.Should().BeTrue();
        result.Error.Should().BeOfType<TaleCancelledError>();
    }

    [Test]
    public async Task Tell_SameChapterTwice_RunsBoth()
    {
        // Arrange
        var handler = new SameChapterTwiceTale(_serviceProvider, GetLogger<SameChapterTwiceTale>());

        // Act
        var result = await handler.Handle(new EngineTestInput { Value = 0 }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.Context.ExecutionOrder.Should().Equal("Chapter1", "Chapter1");
        handler.Context.Value.Should().Be(2);
    }

    [Test]
    public async Task Tell_SameNameDifferentNamespaces_RunsBoth()
    {
        // Arrange
        var handler = new SameNameChaptersTale(_serviceProvider, GetLogger<SameNameChaptersTale>());

        // Act
        var result = await handler.Handle(new EngineTestInput { Value = 0 }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.Context.ExecutionOrder.Should().Equal("Inbound", "Outbound");
    }

    [Test]
    public async Task HistoryKey_GenericChapter_HasNoAssemblyVersion()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<TestService>();
        services.AddSolTale(assemblies: typeof(TaleEngineTests).Assembly);
        using var sp = services.BuildServiceProvider();
        var manager = sp.GetRequiredService<TaleManager>();

        // Act
        var start = await manager.StartStory<GenericChapterTale, EngineTestInput, EngineTesTContext, EngineTestOutput>(
            new EngineTestInput { Value = 1 });

        // Assert
        var chapter = start.Data!.History.Should().ContainSingle().Subject;
        chapter.StepIndex.Should().Be(0);
        chapter.ChapterTypeName.Should().Be(
            "SolTechnology.Core.Tale.Tests.GenericChapter`1[System.Collections.Generic.List`1[SolTechnology.Core.Tale.Tests.EngineTestInput]]");
        $"{chapter.StepIndex}:{chapter.ChapterTypeName}".Should().NotContainAny("Version=", "Culture=", "PublicKeyToken=");
    }

    [Test]
    public async Task TaleEngine_ShouldReturnSingleError_WhenOnlyOneChapterFails()
    {
        // Arrange
        var handler = new SingleErrorTale(_serviceProvider, GetLogger<SingleErrorTale>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeOfType<AggregateError>();
        result.Error!.Message.Should().Be("Single chapter error");
    }

    [Test]
    public async Task TaleEngine_ShouldNotExecuteChapter_WhenNotRegisteredInDI()
    {
        // Arrange - Create a tale that references an unregistered chapter
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        // Intentionally NOT registering UnregisteredChapter
        var serviceProvider = services.BuildServiceProvider();

        var handler = new UnregisteredChapterTale(serviceProvider,
            serviceProvider.GetRequiredService<ILogger<UnregisteredChapterTale>>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("not registered in DI container");
    }

    private ILogger<T> GetLogger<T>()
    {
        return _serviceProvider.GetRequiredService<ILogger<T>>();
    }
}


public class EngineTestTale : TaleHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public EngineTestTale(IServiceProvider sp, ILogger<EngineTestTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<EngineTestChapter1>()
            .Read<EngineTestChapter2>()
            .Read<EngineTestChapter3>()
            .Finale(ctx => ctx.Output);
}

public class MultipleErrorsTale : TaleHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public MultipleErrorsTale(
        IServiceProvider sp,
        ILogger<MultipleErrorsTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<EngineTestFailingChapter>()
            .Read<EngineTestFailingChapter2>()
            .Read<EngineTestFailingChapter3>()
            .Finale(ctx => ctx.Output);
}

public class SingleErrorTale : TaleHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public SingleErrorTale(IServiceProvider sp, ILogger<SingleErrorTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<EngineTestChapter1>()
            .Read<EngineTestSingleErrorChapter>()
            .Read<EngineTestChapter3>()
            .Finale(ctx => ctx.Output);
}

public class ThrowingTale : TaleHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public ThrowingTale(IServiceProvider sp, ILogger<ThrowingTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<EngineTestThrowingChapter>()
            .Finale(ctx => ctx.Output);
}

public class LongRunningTale : TaleHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public LongRunningTale(IServiceProvider sp, ILogger<LongRunningTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<EngineTestChapter1>()
            .Finale(ctx => ctx.Output);
}

public class BlockingTale : TaleHandler<EngineTestInput, BlockingContext, EngineTestOutput>
{
    public BlockingTale(IServiceProvider sp, ILogger<BlockingTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<EngineTestBlockingChapter>()
            .Finale(ctx => ctx.Output);
}

public class EngineTestBlockingChapter : Chapter<BlockingContext>
{
    public override async Task<Result> Read(BlockingContext context, CancellationToken cancellationToken)
    {
        context.Received = cancellationToken;
        context.Work = Task.Delay(Timeout.Infinite, cancellationToken);
        await context.Work;
        return Result.Success();
    }
}

public class BlockingContext : Context<EngineTestInput, EngineTestOutput>
{
    [JsonIgnore]
    public CancellationToken Received { get; set; }

    [JsonIgnore]
    public Task? Work { get; set; }
}

public class SameChapterTwiceTale : TaleHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public SameChapterTwiceTale(IServiceProvider sp, ILogger<SameChapterTwiceTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<EngineTestChapter1>()
            .Read<EngineTestChapter1>()
            .Finale(ctx => ctx.Output);
}

public class SameNameChaptersTale : TaleHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public SameNameChaptersTale(IServiceProvider sp, ILogger<SameNameChaptersTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<Inbound.RecordStepChapter>()
            .Read<Outbound.RecordStepChapter>()
            .Finale(ctx => ctx.Output);
}

public class GenericChapterTale : TaleHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public GenericChapterTale(IServiceProvider sp, ILogger<GenericChapterTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<GenericChapter<List<EngineTestInput>>>()
            .Finale(ctx => ctx.Output);
}

public class GenericChapter<TPayload> : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        context.ExecutionOrder.Add(typeof(TPayload).Name);
        return Result.SuccessAsTask();
    }
}

public class UnregisteredChapterTale : TaleHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public UnregisteredChapterTale(IServiceProvider sp, ILogger<UnregisteredChapterTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<EngineTestOutput> Tell() =>
        Open<UnregisteredChapter>()
            .Finale(ctx => ctx.Output);
}



public class EngineTestChapter1 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        context.ExecutionOrder.Add("Chapter1");
        context.Value += 1;
        return Result.SuccessAsTask();
    }
}

public class EngineTestChapter2 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        context.ExecutionOrder.Add("Chapter2");
        context.Value += 2;
        return Result.SuccessAsTask();
    }
}

public class EngineTestChapter3 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        context.ExecutionOrder.Add("Chapter3");
        context.Value += 3;
        return Result.SuccessAsTask();
    }
}

public class EngineTestFailingChapter : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        context.ExecutionOrder.Add("Chapter1");
        return Result.FailAsTask("Error from Chapter 1");
    }
}

public class EngineTestFailingChapter2 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        context.ExecutionOrder.Add("Chapter2");
        return Result.FailAsTask("Error from Chapter 2");
    }
}

public class EngineTestFailingChapter3 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        context.ExecutionOrder.Add("Chapter3");
        return Result.FailAsTask("Error from Chapter 3");
    }
}

public class EngineTestSingleErrorChapter : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        context.ExecutionOrder.Add("Chapter2");
        return Result.FailAsTask("Single chapter error");
    }
}

public class EngineTestThrowingChapter : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Intentional exception for testing");
    }
}

public class UnregisteredChapter : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
    {
        return Result.SuccessAsTask();
    }
}



public class EngineTestInput
{
    public int Value { get; set; }
}

public class EngineTestOutput
{
    public int FinalValue { get; set; }
}

public class EngineTesTContext : Context<EngineTestInput, EngineTestOutput>
{
    public int Value { get; set; }
    public List<string> ExecutionOrder { get; set; } = new();
}

