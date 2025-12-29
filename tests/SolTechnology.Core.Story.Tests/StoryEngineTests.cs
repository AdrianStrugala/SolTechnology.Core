using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Story;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Tests for StoryEngine internal orchestration.
/// Verifies error aggregation, chapter execution flow, and engine state management.
/// </summary>
[TestFixture]
public class StoryEngineTests
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
    public async Task StoryEngine_ShouldExecuteAllChapters_InSequence()
    {
        // Arrange
        var handler = new EngineTestStory(_serviceProvider, GetLogger<EngineTestStory>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.Context.ExecutionOrder.Should().Equal(new[] { "Chapter1", "Chapter2", "Chapter3" });
    }

    [Test]
    public async Task StoryEngine_ShouldAggregateErrors_WhenMultipleChaptersFail()
    {
        // Arrange - create service provider with StopOnFirstError = false
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(new StoryOptions { StopOnFirstError = false });
        services.AddTransient<EngineTestFailingChapter>();
        services.AddTransient<EngineTestFailingChapter2>();
        services.AddTransient<EngineTestFailingChapter3>();
        var sp = services.BuildServiceProvider();

        var handler = new MultipleErrorsStory(sp, sp.GetRequiredService<ILogger<MultipleErrorsStory>>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AggregateError>();

        var aggregateError = result.Error as AggregateError;
        aggregateError!.InnerErrors.Should().HaveCount(3);
        aggregateError.InnerErrors.Select(e => e.Message).Should().Contain("Error from Chapter 1");
        aggregateError.InnerErrors.Select(e => e.Message).Should().Contain("Error from Chapter 2");
        aggregateError.InnerErrors.Select(e => e.Message).Should().Contain("Error from Chapter 3");
    }

    [Test]
    public async Task StoryEngine_ShouldStopOnFirstError_WhenConfigured()
    {
        // Arrange - create service provider with StopOnFirstError = true
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(new StoryOptions { StopOnFirstError = true });
        services.AddTransient<EngineTestFailingChapter>();
        services.AddTransient<EngineTestFailingChapter2>();
        services.AddTransient<EngineTestFailingChapter3>();
        var sp = services.BuildServiceProvider();

        var handler = new MultipleErrorsStory(sp, sp.GetRequiredService<ILogger<MultipleErrorsStory>>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeOfType<AggregateError>(); // Should be single error
        result.Error!.Message.Should().Be("Error from Chapter 1");

        // Only first chapter should have executed
        handler.Context.ExecutionOrder.Should().Equal(new[] { "Chapter1" });
    }

    [Test]
    public async Task StoryEngine_ShouldContinueAfterErrors_WhenStopOnFirstErrorIsFalse()
    {
        // Arrange - create service provider with StopOnFirstError = false
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(new StoryOptions { StopOnFirstError = false });
        services.AddTransient<EngineTestFailingChapter>();
        services.AddTransient<EngineTestFailingChapter2>();
        services.AddTransient<EngineTestFailingChapter3>();
        var sp = services.BuildServiceProvider();

        var handler = new MultipleErrorsStory(sp, sp.GetRequiredService<ILogger<MultipleErrorsStory>>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();

        // All chapters should have executed despite errors
        handler.Context.ExecutionOrder.Should().Equal(new[] { "Chapter1", "Chapter2", "Chapter3" });
    }

    [Test]
    public async Task StoryEngine_ShouldConvertExceptions_ToErrors()
    {
        // Arrange
        var handler = new ThrowingStory(_serviceProvider, GetLogger<ThrowingStory>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("Intentional exception");
    }

    [Test]
    public async Task StoryEngine_ShouldHandleCancellation()
    {
        // Arrange
        var handler = new LongRunningStory(_serviceProvider, GetLogger<LongRunningStory>());
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
    public async Task StoryEngine_ShouldReturnSingleError_WhenOnlyOneChapterFails()
    {
        // Arrange
        var handler = new SingleErrorStory(_serviceProvider, GetLogger<SingleErrorStory>());
        var input = new EngineTestInput { Value = 1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeOfType<AggregateError>();
        result.Error!.Message.Should().Be("Single chapter error");
    }

    [Test]
    public async Task StoryEngine_ShouldNotExecuteChapter_WhenNotRegisteredInDI()
    {
        // Arrange - Create a story that references an unregistered chapter
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        // Intentionally NOT registering UnregisteredChapter
        var serviceProvider = services.BuildServiceProvider();

        var handler = new UnregisteredChapterStory(serviceProvider,
            serviceProvider.GetRequiredService<ILogger<UnregisteredChapterStory>>());
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

#region Test Stories

public class EngineTestStory : StoryHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public EngineTestStory(IServiceProvider sp, ILogger<EngineTestStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<EngineTestChapter1>();
        await ReadChapter<EngineTestChapter2>();
        await ReadChapter<EngineTestChapter3>();
    }
}

public class MultipleErrorsStory : StoryHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public MultipleErrorsStory(
        IServiceProvider sp,
        ILogger<MultipleErrorsStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<EngineTestFailingChapter>();
        await ReadChapter<EngineTestFailingChapter2>();
        await ReadChapter<EngineTestFailingChapter3>();
    }
}

public class SingleErrorStory : StoryHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public SingleErrorStory(IServiceProvider sp, ILogger<SingleErrorStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<EngineTestChapter1>();
        await ReadChapter<EngineTestSingleErrorChapter>();
        await ReadChapter<EngineTestChapter3>();
    }
}

public class ThrowingStory : StoryHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public ThrowingStory(IServiceProvider sp, ILogger<ThrowingStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<EngineTestThrowingChapter>();
    }
}

public class LongRunningStory : StoryHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public LongRunningStory(IServiceProvider sp, ILogger<LongRunningStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<EngineTestChapter1>();
    }
}

public class UnregisteredChapterStory : StoryHandler<EngineTestInput, EngineTesTContext, EngineTestOutput>
{
    public UnregisteredChapterStory(IServiceProvider sp, ILogger<UnregisteredChapterStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<UnregisteredChapter>();
    }
}

#endregion

#region Test Chapters

public class EngineTestChapter1 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context)
    {
        context.ExecutionOrder.Add("Chapter1");
        context.Value += 1;
        return Result.SuccessAsTask();
    }
}

public class EngineTestChapter2 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context)
    {
        context.ExecutionOrder.Add("Chapter2");
        context.Value += 2;
        return Result.SuccessAsTask();
    }
}

public class EngineTestChapter3 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context)
    {
        context.ExecutionOrder.Add("Chapter3");
        context.Value += 3;
        return Result.SuccessAsTask();
    }
}

public class EngineTestFailingChapter : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context)
    {
        context.ExecutionOrder.Add("Chapter1");
        return Result.FailAsTask("Error from Chapter 1");
    }
}

public class EngineTestFailingChapter2 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context)
    {
        context.ExecutionOrder.Add("Chapter2");
        return Result.FailAsTask("Error from Chapter 2");
    }
}

public class EngineTestFailingChapter3 : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context)
    {
        context.ExecutionOrder.Add("Chapter3");
        return Result.FailAsTask("Error from Chapter 3");
    }
}

public class EngineTestSingleErrorChapter : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context)
    {
        context.ExecutionOrder.Add("Chapter2");
        return Result.FailAsTask("Single chapter error");
    }
}

public class EngineTestThrowingChapter : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context)
    {
        throw new InvalidOperationException("Intentional exception for testing");
    }
}

public class UnregisteredChapter : Chapter<EngineTesTContext>
{
    public override Task<Result> Read(EngineTesTContext context)
    {
        return Result.SuccessAsTask();
    }
}

#endregion

#region Test Models

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

#endregion
