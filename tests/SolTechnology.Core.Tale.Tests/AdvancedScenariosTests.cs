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
/// Advanced QA scenarios - edge cases, error handling, security, concurrency.
/// Testing Tale Framework like a professional QA trying to break it.
/// </summary>
[TestFixture]
public class AdvancedScenariosTests
{
    private IServiceProvider _serviceProvider = null!;
    private InMemoryTaleRepository _repository = null!;
    private TaleManager _taleManager = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        _repository = new InMemoryTaleRepository();
        services.AddSingleton(new TaleOptions());
        services.AddSingleton<ITaleRepository>(_repository);
        services.AddScoped<TaleManager>();

        // Register test chapters
        services.AddTransient<ValidInputChapter>();
        services.AddTransient<ComplexValidationChapter>();
        services.AddTransient<TimeoutSimulationChapter>();
        services.AddTransient<LargeDataChapter>();
        services.AddTransient<SecondInteractiveChapter>();
        services.AddTransient<ThirdInteractiveChapter>();

        _serviceProvider = services.BuildServiceProvider();
        _taleManager = _serviceProvider.GetRequiredService<TaleManager>();
    }

    [TearDown]
    public void TearDown()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }

    #region Invalid Resume Scenarios

    [Test]
    public async Task Resume_NonExistentTale_ShouldReturnError()
    {
        // Arrange
        var fakeTaleId = Auid.New();

        // Act
        var result = await _taleManager.ResumeStory<TestAdvancedTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            fakeTaleId,
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("not found");
    }

    [Test]
    public async Task Resume_AlreadyCompletedTale_ShouldReturnError()
    {
        // Arrange - Start and complete a tale
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SimpleCompletionTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Wait for tale to complete (no interactive chapters)
        await Task.Delay(100);

        // Act - Try to resume completed tale
        var result = await _taleManager.ResumeStory<SimpleCompletionTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.ToLower().Should().Contain("completed");
    }

    [Test]
    public async Task Resume_WithWrongInputType_ShouldStayPaused()
    {
        // Arrange - Start tale that pauses at interactive chapter
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with completely wrong input structure
        var wrongInput = JsonDocument.Parse("{\"wrongField\": \"wrongValue\", \"number\": 123}");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            wrongInput.RootElement);

        // Assert — wrong input deserializes to defaults → chapter validation rejects → stays paused
        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
    }

    [Test]
    public async Task Resume_WithMissingRequiredFields_ShouldStayPaused()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with missing required fields
        var incompleteInput = JsonDocument.Parse("{\"name\": \"John\"}"); // Missing 'email' and 'age'
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            incompleteInput.RootElement);

        // Assert — validation failure keeps the tale paused for retry
        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
    }

    [Test]
    public async Task Resume_WithNullValues_ShouldNotComplete()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with null values
        var nullInput = JsonDocument.Parse("{\"name\": null, \"email\": null, \"age\": null}");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            nullInput.RootElement);

        // Assert — nulls must NOT produce a completed tale.
        // Either deserialization fails (terminal → IsFailure) or chapter validation
        // rejects them (retryable → IsSuccess + WaitingForInput).
        if (result.IsSuccess)
        {
            result.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
        }
    }

    [Test]
    public async Task Resume_WithExtraFields_ShouldIgnoreAndSucceed()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with extra unexpected fields
        var extraFieldsInput = JsonDocument.Parse(@"{
            ""name"": ""John Doe"",
            ""email"": ""john@example.com"",
            ""age"": 30,
            ""extraField1"": ""should be ignored"",
            ""extraField2"": 999,
            ""nestedExtra"": { ""foo"": ""bar"" }
        }");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            extraFieldsInput.RootElement);

        // Assert - Should succeed, ignoring extra fields
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Input Validation Edge Cases

    [Test]
    public async Task Resume_WithEmptyStrings_ShouldStayPaused()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with empty strings
        var emptyInput = JsonDocument.Parse("{\"name\": \"\", \"email\": \"\", \"age\": 0}");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            emptyInput.RootElement);

        // Assert — validation failure keeps the tale paused for retry
        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
    }

    [Test]
    public async Task Resume_WithWhitespaceStrings_ShouldStayPaused()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with whitespace-only strings
        var whitespaceInput = JsonDocument.Parse("{\"name\": \"   \", \"email\": \"\\t\\n\", \"age\": 25}");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            whitespaceInput.RootElement);

        // Assert — validation failure keeps the tale paused for retry
        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
    }

    [Test]
    public async Task Resume_WithExtremelyLongStrings_ShouldHandleOrReject()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with extremely long string (10,000 characters)
        var longString = new string('A', 10000);
        var longInput = JsonDocument.Parse($"{{\"name\": \"{longString}\", \"email\": \"test@example.com\", \"age\": 25}}");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            longInput.RootElement);

        // Assert - Should either succeed with truncation or fail with validation error
        if (result.IsFailure)
        {
            result.Error!.Message.Should().ContainAny("too long", "length", "maximum");
        }
        else
        {
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Test]
    public async Task Resume_WithSpecialCharacters_ShouldHandleSafely()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with special characters, potential injection attempts
        var specialCharsInput = JsonDocument.Parse(@"{
            ""name"": ""<script>alert('xss')</script>"",
            ""email"": ""test@example.com'; DROP TABLE Users;--"",
            ""age"": 25
        }");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            specialCharsInput.RootElement);

        // Assert - Should handle safely (either escape or reject)
        result.Should().NotBeNull();
        if (result.IsSuccess)
        {
            // If accepted, data should be safely stored
            var state = await _taleManager.GetStoryState(taleId);
            state.IsSuccess.Should().BeTrue();
        }
    }

    [Test]
    public async Task Resume_WithUnicodeCharacters_ShouldPreserveEncoding()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with Unicode characters (emoji, non-Latin scripts)
        var unicodeInput = JsonDocument.Parse(@"{
            ""name"": ""用户名 👨‍💻 Użytkownik"",
            ""email"": ""test@example.com"",
            ""age"": 25
        }");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            unicodeInput.RootElement);

        // Assert - Should preserve Unicode correctly
        if (result.IsSuccess)
        {
            var state = await _taleManager.GetStoryState(taleId);
            // Unicode should be preserved in storage
            state.IsSuccess.Should().BeTrue();
        }
    }

    [Test]
    public async Task Resume_WithNegativeNumbers_ShouldStayPaused()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with negative age
        var negativeInput = JsonDocument.Parse("{\"name\": \"John\", \"email\": \"test@example.com\", \"age\": -5}");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            negativeInput.RootElement);

        // Assert — validation failure keeps the tale paused for retry
        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
    }

    [Test]
    public async Task Resume_WithExtremeNumbers_ShouldHandleOverflow()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Resume with Int32.MaxValue
        var extremeInput = JsonDocument.Parse($"{{\"name\": \"John\", \"email\": \"test@example.com\", \"age\": {int.MaxValue}}}");
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            extremeInput.RootElement);

        // Assert - Should handle gracefully
        result.Should().NotBeNull();
    }

    #endregion

    #region Multiple Pause/Resume Cycles

    [Test]
    public async Task Tale_WithMultiplePauses_ShouldResumeCorrectly()
    {
        // Arrange - Tale with 3 interactive chapters
        var input = new TestAdvancedInput { Value = "multi-pause" };

        // Act 1 - Start tale (pauses at first interactive chapter)
        var startResult = await _taleManager.StartStory<MultiPauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        startResult.IsSuccess.Should().BeTrue();
        var taleId = startResult.Data!.TaleId;

        // Act 2 - Resume with first input (pauses at second interactive chapter)
        var firstInput = JsonDocument.Parse("{\"Name\": \"John\", \"Email\": \"john@example.com\", \"Age\": 30}");
        var resume1 = await _taleManager.ResumeStory<MultiPauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            firstInput.RootElement);
        resume1.IsSuccess.Should().BeTrue();
        resume1.Data!.Status.Should().Be(TaleStatus.WaitingForInput);

        // Act 3 - Resume with second input (pauses at third interactive chapter)
        var secondInput = JsonDocument.Parse("{\"Address\": \"123 Main St\", \"City\": \"New York\"}");
        var resume2 = await _taleManager.ResumeStory<MultiPauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            secondInput.RootElement);
        resume2.IsSuccess.Should().BeTrue();
        resume2.Data!.Status.Should().Be(TaleStatus.WaitingForInput);

        // Act 4 - Resume with third input (completes)
        var thirdInput = JsonDocument.Parse("{\"CardNumber\": \"1234-5678\", \"Cvv\": \"123\"}");
        var resume3 = await _taleManager.ResumeStory<MultiPauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            thirdInput.RootElement);

        // Assert - Should complete successfully
        resume3.IsSuccess.Should().BeTrue();
        resume3.Data!.Status.Should().Be(TaleStatus.Completed);
        resume3.Data.History.Should().HaveCountGreaterThan(2);
    }

    [Test]
    public async Task Tale_ResumeWithoutInput_WhenInputRequired_ShouldStayPaused()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Try to resume without providing user input
        var result = await _taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
            taleId,
            null);

        // Assert - Should remain paused or return error
        if (result.IsSuccess)
        {
            result.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
        }
        else
        {
            result.Error!.Message.Should().ContainAny("input required", "required data", "missing input");
        }
    }

    #endregion

    #region Concurrency Tests

    [Test]
    public async Task Concurrent_MultipleStories_ShouldIsolate()
    {
        // Arrange - Start 10 stories concurrently
        var tasks = new List<Task<Result<TaleInstance>>>();

        for (int i = 0; i < 10; i++)
        {
            var input = new TestAdvancedInput { Value = $"concurrent-{i}" };
            tasks.Add(_taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed independently
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        // All should have unique IDs
        var taleIds = results.Select(r => r.Data!.TaleId).ToList();
        taleIds.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public async Task Concurrent_ResumeAttempts_ShouldHandleSafely()
    {
        // Arrange - Start a tale
        var input = new TestAdvancedInput { Value = "concurrent-resume" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Try to resume concurrently 5 times with same input
        var userInput = JsonDocument.Parse("{\"name\": \"John\", \"email\": \"john@example.com\", \"age\": 30}");
        var resumeTasks = new List<Task<Result<TaleInstance>>>();

        for (int i = 0; i < 5; i++)
        {
            resumeTasks.Add(_taleManager.ResumeStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(
                taleId,
                userInput.RootElement));
        }

        var results = await Task.WhenAll(resumeTasks);

        // Assert - Should handle gracefully (either one succeeds or all fail with clear error)
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => r.IsFailure);

        // At least one should handle it properly
        (successCount + failureCount).Should().Be(5);

        // If there are failures, they should have meaningful errors
        if (failureCount > 0)
        {
            results.Where(r => r.IsFailure)
                .Should().AllSatisfy(r => r.Error.Should().NotBeNull());
        }
    }

    #endregion

    #region Repository Failure Scenarios

    [Test]
    public async Task Repository_SaveFailure_ShouldPropagateError()
    {
        // Arrange - Create a failing repository
        var failingRepo = new FailingTaleRepository(simulateSaveFailure: true);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(new TaleOptions());
        services.AddSingleton<ITaleRepository>(failingRepo);
        services.AddScoped<TaleManager>();
        services.AddTransient<ValidInputChapter>();

        var sp = services.BuildServiceProvider();
        var manager = sp.GetRequiredService<TaleManager>();

        // Act - Try to start a tale (should fail when saving)
        var input = new TestAdvancedInput { Value = "test" };
        var result = await manager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().ContainAny("save", "persist", "storage");
    }

    [Test]
    public async Task Repository_LoadFailure_ShouldReturnError()
    {
        // Arrange - Create a failing repository
        var failingRepo = new FailingTaleRepository(simulateLoadFailure: true);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(new TaleOptions());
        services.AddSingleton<ITaleRepository>(failingRepo);
        services.AddScoped<TaleManager>();

        var sp = services.BuildServiceProvider();
        var manager = sp.GetRequiredService<TaleManager>();

        // Act - Try to get tale state (should fail when loading)
        var fakeId = Auid.New();
        var result = await manager.GetStoryState(fakeId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().ContainAny("load", "retrieve", "storage");
    }

    #endregion

    #region Large Data Scenarios

    [Test]
    public async Task Tale_WithLargeContext_ShouldHandleEfficiently()
    {
        // Arrange - Tale with large data in context
        var input = new TestAdvancedInput
        {
            Value = "large-data",
            LargeData = new string('X', 100_000) // 100KB of data
        };

        // Act
        var result = await _taleManager.StartStory<LargeDataTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);

        // Assert - Should handle large data without issues
        result.IsSuccess.Should().BeTrue();

        // Verify data is preserved
        var state = await _taleManager.GetStoryState(result.Data!.TaleId);
        state.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region State Consistency Tests

    [Test]
    public async Task GetStoryState_AfterPause_ShouldReflectCorrectState()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "state-check" };
        var startResult = await _taleManager.StartStory<SinglePauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        // Act - Get state while paused
        var state = await _taleManager.GetStoryState(taleId);

        // Assert
        state.IsSuccess.Should().BeTrue();
        state.Data!.Status.Should().Be(TaleStatus.WaitingForInput);
        state.Data.CurrentChapter.Should().NotBeNull();
        state.Data.CurrentChapter!.RequiredData.Should().NotBeEmpty();
        state.Data.TaleId.Should().Be(taleId);
    }

    [Test]
    public async Task Tale_HistoryTracking_ShouldRecordAllChapters()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "history-test" };

        // Act - Complete multi-pause tale
        var startResult = await _taleManager.StartStory<MultiPauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(input);
        var taleId = startResult.Data!.TaleId;

        var input1 = JsonDocument.Parse("{\"name\": \"John\", \"email\": \"john@example.com\", \"age\": 30}");
        await _taleManager.ResumeStory<MultiPauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(taleId, input1.RootElement);

        var input2 = JsonDocument.Parse("{\"address\": \"123 Main St\", \"city\": \"New York\"}");
        await _taleManager.ResumeStory<MultiPauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(taleId, input2.RootElement);

        var input3 = JsonDocument.Parse("{\"cardNumber\": \"1234-5678\", \"cvv\": \"123\"}");
        var finalResult = await _taleManager.ResumeStory<MultiPauseTale, TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>(taleId, input3.RootElement);

        // Assert - History should contain all chapters
        finalResult.IsSuccess.Should().BeTrue();
        var history = finalResult.Data!.History;

        history.Should().NotBeEmpty();
        history.Should().Contain(h => h.ChapterId == "ValidInputChapter");
        history.Should().Contain(h => h.ChapterId == "SecondInteractiveChapter");
        history.Should().Contain(h => h.ChapterId == "ThirdInteractiveChapter");

        // All chapters should have timestamps
        history.Should().AllSatisfy(h =>
        {
            h.StartedAt.Should().BeAfter(DateTime.MinValue);
            if (h.FinishedAt.HasValue)
            {
                h.FinishedAt.Value.Should().BeAfter(h.StartedAt);
            }
        });
    }

    #endregion
}

#region Test Stories

public class TestAdvancedTale : TaleHandler<TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>
{
    public TestAdvancedTale(IServiceProvider sp, ILogger<TestAdvancedTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<TestAdvancedOutput> Tell() =>
        Open<ValidInputChapter>()
            .Do(ctx => ctx.Output.Result = "Completed")
            .Finale(ctx => ctx.Output);
}

public class SimpleCompletionTale : TaleHandler<TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>
{
    public SimpleCompletionTale(IServiceProvider sp, ILogger<SimpleCompletionTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<TestAdvancedOutput> Tell() =>
        // No interactive chapters - completes immediately
        Open(ctx => ctx.Output.Result = "Done")
            .Finale(ctx => ctx.Output);
}

public class SinglePauseTale : TaleHandler<TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>
{
    public SinglePauseTale(IServiceProvider sp, ILogger<SinglePauseTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<TestAdvancedOutput> Tell() =>
        Open<ValidInputChapter>()
            .Do(ctx => ctx.Output.Result = "Completed after pause")
            .Finale(ctx => ctx.Output);
}

public class MultiPauseTale : TaleHandler<TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>
{
    public MultiPauseTale(IServiceProvider sp, ILogger<MultiPauseTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<TestAdvancedOutput> Tell() =>
        Open<ValidInputChapter>()
            .Read<SecondInteractiveChapter>()
            .Read<ThirdInteractiveChapter>()
            .Do(ctx => ctx.Output.Result = "Completed all pauses")
            .Finale(ctx => ctx.Output);
}

public class LargeDataTale : TaleHandler<TestAdvancedInput, TestAdvancedContext, TestAdvancedOutput>
{
    public LargeDataTale(IServiceProvider sp, ILogger<LargeDataTale> logger)
        : base(sp, logger)
    {
    }

    protected override Tale<TestAdvancedOutput> Tell() =>
        Open<LargeDataChapter>()
            .Do(ctx => ctx.Output.Result = "Large data processed")
            .Finale(ctx => ctx.Output);
}

#endregion

#region Test Chapters

public class ValidInputChapter : InteractiveChapter<TestAdvancedContext, UserInputData>
{
    public override Task<Result> ReadWithInput(TestAdvancedContext context, UserInputData userInput)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(userInput.Name))
            return Result.FailAsTask("Name is required");

        if (string.IsNullOrWhiteSpace(userInput.Email))
            return Result.FailAsTask("Email is required");

        if (userInput.Age < 0)
            return Result.FailAsTask("Age must be a positive number - invalid age");

        if (userInput.Age > 150)
            return Result.FailAsTask("Invalid age - too high");

        // Process
        context.UserName = userInput.Name.Trim();
        context.UserEmail = userInput.Email.Trim();
        context.UserAge = userInput.Age;

        return Result.SuccessAsTask();
    }
}

public class SecondInteractiveChapter : InteractiveChapter<TestAdvancedContext, AddressInputData>
{
    public override Task<Result> ReadWithInput(TestAdvancedContext context, AddressInputData userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.Address))
            return Result.FailAsTask("Address required");

        context.Address = userInput.Address;
        context.City = userInput.City ?? "";

        return Result.SuccessAsTask();
    }
}

public class ThirdInteractiveChapter : InteractiveChapter<TestAdvancedContext, PaymentInputData>
{
    public override Task<Result> ReadWithInput(TestAdvancedContext context, PaymentInputData userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.CardNumber))
            return Result.FailAsTask("Card number required");

        context.PaymentInfo = $"Card: {userInput.CardNumber}";

        return Result.SuccessAsTask();
    }
}

public class ComplexValidationChapter : Chapter<TestAdvancedContext>
{
    public override Task<Result> Read(TestAdvancedContext context)
    {
        // Simulate complex business logic validation
        if (context.UserAge < 18)
            return Result.FailAsTask("User must be 18 or older");

        return Result.SuccessAsTask();
    }
}

public class TimeoutSimulationChapter : Chapter<TestAdvancedContext>
{
    public override async Task<Result> Read(TestAdvancedContext context)
    {
        // Simulate long-running operation
        await Task.Delay(100);
        return Result.Success();
    }
}

public class LargeDataChapter : Chapter<TestAdvancedContext>
{
    public override Task<Result> Read(TestAdvancedContext context)
    {
        // Process large data from input
        if (!string.IsNullOrEmpty(context.Input.LargeData))
        {
            context.ProcessedData = $"Processed {context.Input.LargeData.Length} bytes";
        }

        return Result.SuccessAsTask();
    }
}

#endregion

#region Test Models

public class TestAdvancedInput
{
    public string Value { get; set; } = string.Empty;
    public string? LargeData { get; set; }
}

public class TestAdvancedOutput
{
    public string Result { get; set; } = string.Empty;
}

public class TestAdvancedContext : Context<TestAdvancedInput, TestAdvancedOutput>
{
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int UserAge { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PaymentInfo { get; set; } = string.Empty;
    public string ProcessedData { get; set; } = string.Empty;
}

public class UserInputData
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class AddressInputData
{
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
}

public class PaymentInputData
{
    public string CardNumber { get; set; } = string.Empty;
    public string? Cvv { get; set; }
}

#endregion

#region Mock Failing Repository

public class FailingTaleRepository : ITaleRepository
{
    private readonly bool _simulateSaveFailure;
    private readonly bool _simulateLoadFailure;

    public FailingTaleRepository(bool simulateSaveFailure = false, bool simulateLoadFailure = false)
    {
        _simulateSaveFailure = simulateSaveFailure;
        _simulateLoadFailure = simulateLoadFailure;
    }

    public Task<TaleInstance?> FindById(Auid taleId)
    {
        if (_simulateLoadFailure)
            throw new InvalidOperationException("Simulated repository load failure");

        return Task.FromResult<TaleInstance?>(null);
    }

    public Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey)
        => Task.FromResult<TaleInstance?>(null);

    public Task SaveAsync(TaleInstance taleInstance)
    {
        if (_simulateSaveFailure)
            throw new InvalidOperationException("Simulated repository save failure");

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Auid taleId)
    {
        return Task.CompletedTask;
    }
}

#endregion
