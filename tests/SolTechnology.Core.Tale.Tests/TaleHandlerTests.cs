using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// End-to-end tests for TaleHandler.
/// Verifies that stories execute all chapters in sequence and return correct results.
/// </summary>
[TestFixture]
public class TaleHandlerTests
{
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        // Register logging
        services.AddLogging(builder => builder.AddConsole());

        // Register test chapters
        services.AddTransient<CalculateChapter>();
        services.AddTransient<ValidateChapter>();
        services.AddTransient<FormatResultChapter>();
        services.AddTransient<FailingChapter>();
        services.AddTransient<ModifyOutputChapter>();

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
    public async Task TaleHandler_ShouldExecuteAllChapters_InSequence()
    {
        // Arrange
        var handler = new SimpleCalculationTale(_serviceProvider, GetLogger<SimpleCalculationTale>());
        var input = new CalculationInput { Number = 10 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Result.Should().Be("Result: 30"); // (10 + 10) * 1.5 = 30, formatted
    }

    [Test]
    public async Task TaleHandler_ShouldPopulateContext_WithInput()
    {
        // Arrange
        var handler = new SimpleCalculationTale(_serviceProvider, GetLogger<SimpleCalculationTale>());
        var input = new CalculationInput { Number = 5 };

        // Act
        await handler.Handle(input);

        // Assert
        handler.Context.Input.Should().Be(input);
        handler.Context.Input.Number.Should().Be(5);
    }

    [Test]
    public async Task TaleHandler_ShouldReturnOutput_WhenAllChaptersSucceed()
    {
        // Arrange
        var handler = new SimpleCalculationTale(_serviceProvider, GetLogger<SimpleCalculationTale>());
        var input = new CalculationInput { Number = 20 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Result.Should().Be("Result: 45"); // (20 + 10) * 1.5 = 45
        // Step 1: 20 + 10 = 30
        // Step 2: validate (pass)
        // Step 3: 30 * 1.5 = 45, formatted as "Result: 45"
    }

    [Test]
    public async Task TaleHandler_ShouldReturnFailure_WhenChapterFails()
    {
        // Arrange
        var handler = new FailingTale(_serviceProvider, GetLogger<FailingTale>());
        var input = new CalculationInput { Number = -1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("Validation failed");
    }

    [Test]
    public async Task TaleHandler_ShouldStopExecution_WhenChapterFails()
    {
        // Arrange
        var handler = new FailingTale(_serviceProvider, GetLogger<FailingTale>());
        var input = new CalculationInput { Number = -1 };

        // Act
        await handler.Handle(input);

        // Assert - the third chapter should not have been executed
        handler.Context.ChapterExecutionLog.Should().Contain("FailingChapter");
        handler.Context.ChapterExecutionLog.Should().NotContain("FormatResultChapter");
    }

    [Test]
    public async Task TaleHandler_ShouldAllowDirectOutputModification()
    {
        // Arrange
        var handler = new DirectOutputTale(_serviceProvider, GetLogger<DirectOutputTale>());
        var input = new CalculationInput { Number = 42 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Result.Should().Be("Direct output: 42");
    }

    [Test]
    public async Task TaleHandler_ShouldSupportCancellation()
    {
        // Arrange
        var handler = new SimpleCalculationTale(_serviceProvider, GetLogger<SimpleCalculationTale>());
        var input = new CalculationInput { Number = 10 };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await handler.Handle(input, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("cancelled");
    }

    [Test]
    public async Task Handle_TwiceOnSameInstance_ReturnsSecondResult()
    {
        // Arrange
        using var sp = BuildTaleProvider();
        var handler = sp.GetRequiredService<SimpleCalculationTale>();
        await handler.Handle(new CalculationInput { Number = 10 }, CancellationToken.None);

        // Act
        var second = await handler.Handle(new CalculationInput { Number = 20 }, CancellationToken.None);

        // Assert
        second.IsSuccess.Should().BeTrue();
        second.Data!.Result.Should().Be("Result: 45");
        handler.Context.Input.Number.Should().Be(20);
    }

    [Test]
    public async Task Handle_Direct_DoesNotWriteRepository()
    {
        // Arrange
        using var sp = BuildTaleProvider();
        var repository = (InMemoryTaleRepository)sp.GetRequiredService<ITaleRepository>();
        var handler = sp.GetRequiredService<SimpleCalculationTale>();

        // Act
        for (var i = 0; i < 100; i++)
        {
            await handler.Handle(new CalculationInput { Number = i }, CancellationToken.None);
        }

        // Assert
        (await repository.List(take: int.MaxValue)).Should().BeEmpty();
    }

    [Test]
    public async Task Handle_Direct_InteractiveChapterPauses_LogsNotPersistedWarning()
    {
        // Arrange
        var logs = new RecordingLoggerProvider();
        using var sp = BuildTaleProvider(logs);
        var repository = (InMemoryTaleRepository)sp.GetRequiredService<ITaleRepository>();
        var handler = sp.GetRequiredService<LifecycleTaleV1>();

        // Act
        var result = await handler.Handle(new LifecycleInput { Value = 1 }, CancellationToken.None);

        // Assert
        result.Error.Should().BeOfType<TalePausedError>();
        logs.Logs.Should().ContainSingle(log => log.Level == LogLevel.Warning).Which.Message.Should().Be(
            "Tale [LifecycleTaleV1] paused outside TaleManager; the run is not persisted and cannot be resumed.");
        (await repository.List()).Should().BeEmpty();
    }

    [Test]
    public async Task TaleLogs_StartAndResume_BracketEveryPlaceholder()
    {
        // Arrange
        var logs = new RecordingLoggerProvider();
        using var sp = BuildTaleProvider(logs);
        var manager = sp.GetRequiredService<TaleManager>();
        var start = await manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });

        // Act
        await manager.ResumeStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            start.Data!.TaleId, System.Text.Json.JsonSerializer.SerializeToElement(new PausePayload { Token = "t" }));

        // Assert
        var templates = logs.Logs
            .Select(log => log.Properties.FirstOrDefault(p => p.Key == "{OriginalFormat}").Value as string)
            .OfType<string>()
            .Distinct()
            .ToList();
        templates.Should().Contain("Executing chapter: [{ChapterId}]").And.Contain("Tale [{TaleId}] resuming from saved state");
        templates.Should().AllSatisfy(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"(?<!\[)\{[A-Za-z]+\}").Should().BeFalse(t));
    }

    private static ServiceProvider BuildTaleProvider(RecordingLoggerProvider? logs = null)
    {
        var services = new ServiceCollection().AddLogging(builder =>
        {
            if (logs != null)
            {
                builder.AddProvider(logs);
            }
        });
        services.AddSingleton<TestService>();
        services.AddSolTale(assemblies: typeof(TaleHandlerTests).Assembly);
        return services.BuildServiceProvider();
    }

    private ILogger<T> GetLogger<T>()
    {
        return _serviceProvider.GetRequiredService<ILogger<T>>();
    }
}


/// <summary>
/// Simple tale that performs a calculation across 3 chapters.
/// </summary>
public class SimpleCalculationTale : TaleHandler<CalculationInput, CalculationContext, CalculationOutput>
{
    public SimpleCalculationTale(IServiceProvider sp, ILogger<SimpleCalculationTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<CalculationOutput> Tell() =>
        Open<CalculateChapter>()
            .Read<ValidateChapter>()
            .Read<FormatResultChapter>()
            .Finale(ctx => ctx.Output);
}

/// <summary>
/// Tale that fails during validation.
/// </summary>
public class FailingTale : TaleHandler<CalculationInput, CalculationContext, CalculationOutput>
{
    public FailingTale(IServiceProvider sp, ILogger<FailingTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<CalculationOutput> Tell() =>
        Open<CalculateChapter>()
            .Read<FailingChapter>()
            .Read<FormatResultChapter>() // Should not be executed
            .Finale(ctx => ctx.Output);
}

/// <summary>
/// Tale that sets output directly via an inline step.
/// </summary>
public class DirectOutputTale : TaleHandler<CalculationInput, CalculationContext, CalculationOutput>
{
    public DirectOutputTale(IServiceProvider sp, ILogger<DirectOutputTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<CalculationOutput> Tell() =>
        Open<ModifyOutputChapter>()
            .Do(ctx => ctx.Output.Result = $"Direct output: {ctx.Input.Number}")
            .Finale(ctx => ctx.Output);
}



public class CalculateChapter : Chapter<CalculationContext>
{
    public override Task<Result> Read(CalculationContext context, CancellationToken cancellationToken)
    {
        context.ChapterExecutionLog.Add("CalculateChapter");
        context.IntermediateValue = context.Input.Number + 10;
        return Result.SuccessAsTask();
    }
}

public class ValidateChapter : Chapter<CalculationContext>
{
    public override Task<Result> Read(CalculationContext context, CancellationToken cancellationToken)
    {
        context.ChapterExecutionLog.Add("ValidateChapter");

        if (context.IntermediateValue < 0)
        {
            return Result.FailAsTask("Validation failed: negative value");
        }

        return Result.SuccessAsTask();
    }
}

public class FormatResultChapter : Chapter<CalculationContext>
{
    public override Task<Result> Read(CalculationContext context, CancellationToken cancellationToken)
    {
        context.ChapterExecutionLog.Add("FormatResultChapter");
        var finalValue = context.IntermediateValue * 1.5;
        context.Output.Result = $"Result: {finalValue}";
        return Result.SuccessAsTask();
    }
}

public class FailingChapter : Chapter<CalculationContext>
{
    public override Task<Result> Read(CalculationContext context, CancellationToken cancellationToken)
    {
        context.ChapterExecutionLog.Add("FailingChapter");
        return Result.FailAsTask("Validation failed: intentional failure");
    }
}

public class ModifyOutputChapter : Chapter<CalculationContext>
{
    public override Task<Result> Read(CalculationContext context, CancellationToken cancellationToken)
    {
        context.ChapterExecutionLog.Add("ModifyOutputChapter");
        context.Output.Result = "Modified by chapter";
        return Result.SuccessAsTask();
    }
}



public class CalculationInput
{
    public int Number { get; set; }
}

public class CalculationOutput
{
    public string Result { get; set; } = string.Empty;
}

public class CalculationContext : Context<CalculationInput, CalculationOutput>
{
    public int IntermediateValue { get; set; }
    public List<string> ChapterExecutionLog { get; set; } = new();
}

