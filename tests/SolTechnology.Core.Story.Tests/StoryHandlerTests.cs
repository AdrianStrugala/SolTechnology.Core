using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// End-to-end tests for StoryHandler.
/// Verifies that stories execute all chapters in sequence and return correct results.
/// </summary>
[TestFixture]
public class StoryHandlerTests
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
    public async Task StoryHandler_ShouldExecuteAllChapters_InSequence()
    {
        // Arrange
        var handler = new SimpleCalculationStory(_serviceProvider, GetLogger<SimpleCalculationStory>());
        var input = new CalculationInput { Number = 10 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Result.Should().Be("Result: 30"); // (10 + 10) * 1.5 = 30, formatted
    }

    [Test]
    public async Task StoryHandler_ShouldPopulateNarration_WithInput()
    {
        // Arrange
        var handler = new SimpleCalculationStory(_serviceProvider, GetLogger<SimpleCalculationStory>());
        var input = new CalculationInput { Number = 5 };

        // Act
        await handler.Handle(input);

        // Assert
        handler.Narration.Input.Should().Be(input);
        handler.Narration.Input.Number.Should().Be(5);
    }

    [Test]
    public async Task StoryHandler_ShouldReturnOutput_WhenAllChaptersSucceed()
    {
        // Arrange
        var handler = new SimpleCalculationStory(_serviceProvider, GetLogger<SimpleCalculationStory>());
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
    public async Task StoryHandler_ShouldReturnFailure_WhenChapterFails()
    {
        // Arrange
        var handler = new FailingStory(_serviceProvider, GetLogger<FailingStory>());
        var input = new CalculationInput { Number = -1 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("Validation failed");
    }

    [Test]
    public async Task StoryHandler_ShouldStopExecution_WhenChapterFails()
    {
        // Arrange
        var handler = new FailingStory(_serviceProvider, GetLogger<FailingStory>());
        var input = new CalculationInput { Number = -1 };

        // Act
        await handler.Handle(input);

        // Assert - the third chapter should not have been executed
        handler.Narration.ChapterExecutionLog.Should().Contain("FailingChapter");
        handler.Narration.ChapterExecutionLog.Should().NotContain("FormatResultChapter");
    }

    [Test]
    public async Task StoryHandler_ShouldAllowDirectOutputModification()
    {
        // Arrange
        var handler = new DirectOutputStory(_serviceProvider, GetLogger<DirectOutputStory>());
        var input = new CalculationInput { Number = 42 };

        // Act
        var result = await handler.Handle(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Result.Should().Be("Direct output: 42");
    }

    [Test]
    public async Task StoryHandler_ShouldSupportCancellation()
    {
        // Arrange
        var handler = new SimpleCalculationStory(_serviceProvider, GetLogger<SimpleCalculationStory>());
        var input = new CalculationInput { Number = 10 };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await handler.Handle(input, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("cancelled");
    }

    private ILogger<T> GetLogger<T>()
    {
        return _serviceProvider.GetRequiredService<ILogger<T>>();
    }
}

#region Test Stories

/// <summary>
/// Simple story that performs a calculation across 3 chapters.
/// </summary>
public class SimpleCalculationStory : StoryHandler<CalculationInput, CalculationNarration, CalculationOutput>
{
    public SimpleCalculationStory(IServiceProvider sp, ILogger<SimpleCalculationStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<CalculateChapter>();
        await ReadChapter<ValidateChapter>();
        await ReadChapter<FormatResultChapter>();
    }
}

/// <summary>
/// Story that fails during validation.
/// </summary>
public class FailingStory : StoryHandler<CalculationInput, CalculationNarration, CalculationOutput>
{
    public FailingStory(IServiceProvider sp, ILogger<FailingStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<CalculateChapter>();
        await ReadChapter<FailingChapter>();
        await ReadChapter<FormatResultChapter>(); // Should not be executed
    }
}

/// <summary>
/// Story that sets output directly in TellStory.
/// </summary>
public class DirectOutputStory : StoryHandler<CalculationInput, CalculationNarration, CalculationOutput>
{
    public DirectOutputStory(IServiceProvider sp, ILogger<DirectOutputStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<ModifyOutputChapter>();
        Narration.Output.Result = $"Direct output: {Narration.Input.Number}";
    }
}

#endregion

#region Test Chapters

public class CalculateChapter : Chapter<CalculationNarration>
{
    public override Task<Result> Read(CalculationNarration narration)
    {
        narration.ChapterExecutionLog.Add("CalculateChapter");
        narration.IntermediateValue = narration.Input.Number + 10;
        return Result.SuccessAsTask();
    }
}

public class ValidateChapter : Chapter<CalculationNarration>
{
    public override Task<Result> Read(CalculationNarration narration)
    {
        narration.ChapterExecutionLog.Add("ValidateChapter");

        if (narration.IntermediateValue < 0)
        {
            return Result.FailAsTask("Validation failed: negative value");
        }

        return Result.SuccessAsTask();
    }
}

public class FormatResultChapter : Chapter<CalculationNarration>
{
    public override Task<Result> Read(CalculationNarration narration)
    {
        narration.ChapterExecutionLog.Add("FormatResultChapter");
        var finalValue = narration.IntermediateValue * 1.5;
        narration.Output.Result = $"Result: {finalValue}";
        return Result.SuccessAsTask();
    }
}

public class FailingChapter : Chapter<CalculationNarration>
{
    public override Task<Result> Read(CalculationNarration narration)
    {
        narration.ChapterExecutionLog.Add("FailingChapter");
        return Result.FailAsTask("Validation failed: intentional failure");
    }
}

public class ModifyOutputChapter : Chapter<CalculationNarration>
{
    public override Task<Result> Read(CalculationNarration narration)
    {
        narration.ChapterExecutionLog.Add("ModifyOutputChapter");
        narration.Output.Result = "Modified by chapter";
        return Result.SuccessAsTask();
    }
}

#endregion

#region Test Models

public class CalculationInput
{
    public int Number { get; set; }
}

public class CalculationOutput
{
    public string Result { get; set; } = string.Empty;
}

public class CalculationNarration : Narration<CalculationInput, CalculationOutput>
{
    public int IntermediateValue { get; set; }
    public List<string> ChapterExecutionLog { get; set; } = new();
}

#endregion
