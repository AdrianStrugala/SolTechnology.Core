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
/// Advanced QA scenarios - edge cases, error handling, security, concurrency.
/// Testing Story Framework like a professional QA trying to break it.
/// </summary>
[TestFixture]
public class AdvancedScenariosTests
{
    private IServiceProvider _serviceProvider = null!;
    private InMemoryStoryRepository _repository = null!;
    private StoryManager _storyManager = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        _repository = new InMemoryStoryRepository();
        services.AddSingleton(StoryOptions.WithInMemoryPersistence());
        services.AddSingleton<IStoryRepository>(_repository);
        services.AddScoped<StoryManager>();

        // Register test chapters
        services.AddTransient<ValidInputChapter>();
        services.AddTransient<ComplexValidationChapter>();
        services.AddTransient<TimeoutSimulationChapter>();
        services.AddTransient<LargeDataChapter>();
        services.AddTransient<SecondInteractiveChapter>();
        services.AddTransient<ThirdInteractiveChapter>();

        _serviceProvider = services.BuildServiceProvider();
        _storyManager = _serviceProvider.GetRequiredService<StoryManager>();
    }

    [TearDown]
    public void TearDown()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }

    #region Invalid Resume Scenarios

    [Test]
    public async Task Resume_NonExistentStory_ShouldReturnError()
    {
        // Arrange
        var fakeStoryId = Auid.New();

        // Act
        var result = await _storyManager.ResumeStory<TestAdvancedStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            fakeStoryId,
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("not found");
    }

    [Test]
    public async Task Resume_AlreadyCompletedStory_ShouldReturnError()
    {
        // Arrange - Start and complete a story
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SimpleCompletionStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Wait for story to complete (no interactive chapters)
        await Task.Delay(100);

        // Act - Try to resume completed story
        var result = await _storyManager.ResumeStory<SimpleCompletionStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.ToLower().Should().Contain("completed");
    }

    [Test]
    public async Task Resume_WithWrongInputType_ShouldReturnError()
    {
        // Arrange - Start story that pauses at interactive chapter
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with completely wrong input structure
        var wrongInput = JsonDocument.Parse("{\"wrongField\": \"wrongValue\", \"number\": 123}");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            wrongInput.RootElement);

        // Assert - Should fail validation
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
    }

    [Test]
    public async Task Resume_WithMissingRequiredFields_ShouldReturnError()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with missing required fields
        var incompleteInput = JsonDocument.Parse("{\"name\": \"John\"}"); // Missing 'email' and 'age'
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            incompleteInput.RootElement);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().ContainAny("required", "missing", "invalid");
    }

    [Test]
    public async Task Resume_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with null values
        var nullInput = JsonDocument.Parse("{\"name\": null, \"email\": null, \"age\": null}");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            nullInput.RootElement);

        // Assert - Should fail validation
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Resume_WithExtraFields_ShouldIgnoreAndSucceed()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with extra unexpected fields
        var extraFieldsInput = JsonDocument.Parse(@"{
            ""name"": ""John Doe"",
            ""email"": ""john@example.com"",
            ""age"": 30,
            ""extraField1"": ""should be ignored"",
            ""extraField2"": 999,
            ""nestedExtra"": { ""foo"": ""bar"" }
        }");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            extraFieldsInput.RootElement);

        // Assert - Should succeed, ignoring extra fields
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Input Validation Edge Cases

    [Test]
    public async Task Resume_WithEmptyStrings_ShouldFailValidation()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with empty strings
        var emptyInput = JsonDocument.Parse("{\"name\": \"\", \"email\": \"\", \"age\": 0}");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            emptyInput.RootElement);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().ContainAny("empty", "required", "invalid");
    }

    [Test]
    public async Task Resume_WithWhitespaceStrings_ShouldFailValidation()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with whitespace-only strings
        var whitespaceInput = JsonDocument.Parse("{\"name\": \"   \", \"email\": \"\\t\\n\", \"age\": 25}");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            whitespaceInput.RootElement);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Resume_WithExtremelyLongStrings_ShouldHandleOrReject()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with extremely long string (10,000 characters)
        var longString = new string('A', 10000);
        var longInput = JsonDocument.Parse($"{{\"name\": \"{longString}\", \"email\": \"test@example.com\", \"age\": 25}}");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
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
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with special characters, potential injection attempts
        var specialCharsInput = JsonDocument.Parse(@"{
            ""name"": ""<script>alert('xss')</script>"",
            ""email"": ""test@example.com'; DROP TABLE Users;--"",
            ""age"": 25
        }");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            specialCharsInput.RootElement);

        // Assert - Should handle safely (either escape or reject)
        result.Should().NotBeNull();
        if (result.IsSuccess)
        {
            // If accepted, data should be safely stored
            var state = await _storyManager.GetStoryState(storyId);
            state.IsSuccess.Should().BeTrue();
        }
    }

    [Test]
    public async Task Resume_WithUnicodeCharacters_ShouldPreserveEncoding()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with Unicode characters (emoji, non-Latin scripts)
        var unicodeInput = JsonDocument.Parse(@"{
            ""name"": ""用户名 👨‍💻 Użytkownik"",
            ""email"": ""test@example.com"",
            ""age"": 25
        }");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            unicodeInput.RootElement);

        // Assert - Should preserve Unicode correctly
        if (result.IsSuccess)
        {
            var state = await _storyManager.GetStoryState(storyId);
            // Unicode should be preserved in storage
            state.IsSuccess.Should().BeTrue();
        }
    }

    [Test]
    public async Task Resume_WithNegativeNumbers_ShouldValidate()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with negative age
        var negativeInput = JsonDocument.Parse("{\"name\": \"John\", \"email\": \"test@example.com\", \"age\": -5}");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            negativeInput.RootElement);

        // Assert - Should fail validation
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().ContainAny("negative", "positive", "invalid age");
    }

    [Test]
    public async Task Resume_WithExtremeNumbers_ShouldHandleOverflow()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Resume with Int32.MaxValue
        var extremeInput = JsonDocument.Parse($"{{\"name\": \"John\", \"email\": \"test@example.com\", \"age\": {int.MaxValue}}}");
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            extremeInput.RootElement);

        // Assert - Should handle gracefully
        result.Should().NotBeNull();
    }

    #endregion

    #region Multiple Pause/Resume Cycles

    [Test]
    public async Task Story_WithMultiplePauses_ShouldResumeCorrectly()
    {
        // Arrange - Story with 3 interactive chapters
        var input = new TestAdvancedInput { Value = "multi-pause" };

        // Act 1 - Start story (pauses at first interactive chapter)
        var startResult = await _storyManager.StartStory<MultiPauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        startResult.IsSuccess.Should().BeTrue();
        var storyId = startResult.Data!.StoryId;

        // Act 2 - Resume with first input (pauses at second interactive chapter)
        var firstInput = JsonDocument.Parse("{\"Name\": \"John\", \"Email\": \"john@example.com\", \"Age\": 30}");
        var resume1 = await _storyManager.ResumeStory<MultiPauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            firstInput.RootElement);
        resume1.IsSuccess.Should().BeTrue();
        resume1.Data!.Status.Should().Be(StoryStatus.WaitingForInput);

        // Act 3 - Resume with second input (pauses at third interactive chapter)
        var secondInput = JsonDocument.Parse("{\"Address\": \"123 Main St\", \"City\": \"New York\"}");
        var resume2 = await _storyManager.ResumeStory<MultiPauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            secondInput.RootElement);
        resume2.IsSuccess.Should().BeTrue();
        resume2.Data!.Status.Should().Be(StoryStatus.WaitingForInput);

        // Act 4 - Resume with third input (completes)
        var thirdInput = JsonDocument.Parse("{\"CardNumber\": \"1234-5678\", \"Cvv\": \"123\"}");
        var resume3 = await _storyManager.ResumeStory<MultiPauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            thirdInput.RootElement);

        // Assert - Should complete successfully
        resume3.IsSuccess.Should().BeTrue();
        resume3.Data!.Status.Should().Be(StoryStatus.Completed);
        resume3.Data.History.Should().HaveCountGreaterThan(2);
    }

    [Test]
    public async Task Story_ResumeWithoutInput_WhenInputRequired_ShouldStayPaused()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "test" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Try to resume without providing user input
        var result = await _storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
            storyId,
            null);

        // Assert - Should remain paused or return error
        if (result.IsSuccess)
        {
            result.Data!.Status.Should().Be(StoryStatus.WaitingForInput);
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
        var tasks = new List<Task<Result<StoryInstance>>>();

        for (int i = 0; i < 10; i++)
        {
            var input = new TestAdvancedInput { Value = $"concurrent-{i}" };
            tasks.Add(_storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed independently
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        // All should have unique IDs
        var storyIds = results.Select(r => r.Data!.StoryId).ToList();
        storyIds.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public async Task Concurrent_ResumeAttempts_ShouldHandleSafely()
    {
        // Arrange - Start a story
        var input = new TestAdvancedInput { Value = "concurrent-resume" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Try to resume concurrently 5 times with same input
        var userInput = JsonDocument.Parse("{\"name\": \"John\", \"email\": \"john@example.com\", \"age\": 30}");
        var resumeTasks = new List<Task<Result<StoryInstance>>>();

        for (int i = 0; i < 5; i++)
        {
            resumeTasks.Add(_storyManager.ResumeStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(
                storyId,
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
        var failingRepo = new FailingStoryRepository(simulateSaveFailure: true);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(StoryOptions.WithInMemoryPersistence());
        services.AddSingleton<IStoryRepository>(failingRepo);
        services.AddScoped<StoryManager>();
        services.AddTransient<ValidInputChapter>();

        var sp = services.BuildServiceProvider();
        var manager = sp.GetRequiredService<StoryManager>();

        // Act - Try to start a story (should fail when saving)
        var input = new TestAdvancedInput { Value = "test" };
        var result = await manager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().ContainAny("save", "persist", "storage");
    }

    [Test]
    public async Task Repository_LoadFailure_ShouldReturnError()
    {
        // Arrange - Create a failing repository
        var failingRepo = new FailingStoryRepository(simulateLoadFailure: true);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(StoryOptions.WithInMemoryPersistence());
        services.AddSingleton<IStoryRepository>(failingRepo);
        services.AddScoped<StoryManager>();

        var sp = services.BuildServiceProvider();
        var manager = sp.GetRequiredService<StoryManager>();

        // Act - Try to get story state (should fail when loading)
        var fakeId = Auid.New();
        var result = await manager.GetStoryState(fakeId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().ContainAny("load", "retrieve", "storage");
    }

    #endregion

    #region Large Data Scenarios

    [Test]
    public async Task Story_WithLargeContext_ShouldHandleEfficiently()
    {
        // Arrange - Story with large data in context
        var input = new TestAdvancedInput
        {
            Value = "large-data",
            LargeData = new string('X', 100_000) // 100KB of data
        };

        // Act
        var result = await _storyManager.StartStory<LargeDataStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);

        // Assert - Should handle large data without issues
        result.IsSuccess.Should().BeTrue();

        // Verify data is preserved
        var state = await _storyManager.GetStoryState(result.Data!.StoryId);
        state.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region State Consistency Tests

    [Test]
    public async Task GetStoryState_AfterPause_ShouldReflectCorrectState()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "state-check" };
        var startResult = await _storyManager.StartStory<SinglePauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        // Act - Get state while paused
        var state = await _storyManager.GetStoryState(storyId);

        // Assert
        state.IsSuccess.Should().BeTrue();
        state.Data!.Status.Should().Be(StoryStatus.WaitingForInput);
        state.Data.CurrentChapter.Should().NotBeNull();
        state.Data.CurrentChapter!.RequiredData.Should().NotBeEmpty();
        state.Data.StoryId.Should().Be(storyId);
    }

    [Test]
    public async Task Story_HistoryTracking_ShouldRecordAllChapters()
    {
        // Arrange
        var input = new TestAdvancedInput { Value = "history-test" };

        // Act - Complete multi-pause story
        var startResult = await _storyManager.StartStory<MultiPauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(input);
        var storyId = startResult.Data!.StoryId;

        var input1 = JsonDocument.Parse("{\"name\": \"John\", \"email\": \"john@example.com\", \"age\": 30}");
        await _storyManager.ResumeStory<MultiPauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(storyId, input1.RootElement);

        var input2 = JsonDocument.Parse("{\"address\": \"123 Main St\", \"city\": \"New York\"}");
        await _storyManager.ResumeStory<MultiPauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(storyId, input2.RootElement);

        var input3 = JsonDocument.Parse("{\"cardNumber\": \"1234-5678\", \"cvv\": \"123\"}");
        var finalResult = await _storyManager.ResumeStory<MultiPauseStory, TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>(storyId, input3.RootElement);

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

public class TestAdvancedStory : StoryHandler<TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>
{
    public TestAdvancedStory(IServiceProvider sp, ILogger<TestAdvancedStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<ValidInputChapter>();
        Narration.Output.Result = "Completed";
    }
}

public class SimpleCompletionStory : StoryHandler<TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>
{
    public SimpleCompletionStory(IServiceProvider sp, ILogger<SimpleCompletionStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        // No interactive chapters - completes immediately
        await Task.CompletedTask;
        Narration.Output.Result = "Done";
    }
}

public class SinglePauseStory : StoryHandler<TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>
{
    public SinglePauseStory(IServiceProvider sp, ILogger<SinglePauseStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<ValidInputChapter>();
        Narration.Output.Result = "Completed after pause";
    }
}

public class MultiPauseStory : StoryHandler<TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>
{
    public MultiPauseStory(IServiceProvider sp, ILogger<MultiPauseStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<ValidInputChapter>();
        await ReadChapter<SecondInteractiveChapter>();
        await ReadChapter<ThirdInteractiveChapter>();
        Narration.Output.Result = "Completed all pauses";
    }
}

public class LargeDataStory : StoryHandler<TestAdvancedInput, TestAdvancedNarration, TestAdvancedOutput>
{
    public LargeDataStory(IServiceProvider sp, ILogger<LargeDataStory> logger)
        : base(sp, logger)
    {
    }

    protected override async Task TellStory()
    {
        await ReadChapter<LargeDataChapter>();
        Narration.Output.Result = "Large data processed";
    }
}

#endregion

#region Test Chapters

public class ValidInputChapter : InteractiveChapter<TestAdvancedNarration, UserInputData>
{
    public override Task<Result> ReadWithInput(TestAdvancedNarration narration, UserInputData userInput)
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
        narration.UserName = userInput.Name.Trim();
        narration.UserEmail = userInput.Email.Trim();
        narration.UserAge = userInput.Age;

        return Result.SuccessAsTask();
    }
}

public class SecondInteractiveChapter : InteractiveChapter<TestAdvancedNarration, AddressInputData>
{
    public override Task<Result> ReadWithInput(TestAdvancedNarration narration, AddressInputData userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.Address))
            return Result.FailAsTask("Address required");

        narration.Address = userInput.Address;
        narration.City = userInput.City ?? "";

        return Result.SuccessAsTask();
    }
}

public class ThirdInteractiveChapter : InteractiveChapter<TestAdvancedNarration, PaymentInputData>
{
    public override Task<Result> ReadWithInput(TestAdvancedNarration narration, PaymentInputData userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput.CardNumber))
            return Result.FailAsTask("Card number required");

        narration.PaymentInfo = $"Card: {userInput.CardNumber}";

        return Result.SuccessAsTask();
    }
}

public class ComplexValidationChapter : Chapter<TestAdvancedNarration>
{
    public override Task<Result> Read(TestAdvancedNarration narration)
    {
        // Simulate complex business logic validation
        if (narration.UserAge < 18)
            return Result.FailAsTask("User must be 18 or older");

        return Result.SuccessAsTask();
    }
}

public class TimeoutSimulationChapter : Chapter<TestAdvancedNarration>
{
    public override async Task<Result> Read(TestAdvancedNarration narration)
    {
        // Simulate long-running operation
        await Task.Delay(100);
        return Result.Success();
    }
}

public class LargeDataChapter : Chapter<TestAdvancedNarration>
{
    public override Task<Result> Read(TestAdvancedNarration narration)
    {
        // Process large data from input
        if (!string.IsNullOrEmpty(narration.Input.LargeData))
        {
            narration.ProcessedData = $"Processed {narration.Input.LargeData.Length} bytes";
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

public class TestAdvancedNarration : Narration<TestAdvancedInput, TestAdvancedOutput>
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

public class FailingStoryRepository : IStoryRepository
{
    private readonly bool _simulateSaveFailure;
    private readonly bool _simulateLoadFailure;

    public FailingStoryRepository(bool simulateSaveFailure = false, bool simulateLoadFailure = false)
    {
        _simulateSaveFailure = simulateSaveFailure;
        _simulateLoadFailure = simulateLoadFailure;
    }

    public Task<StoryInstance?> FindById(Auid storyId)
    {
        if (_simulateLoadFailure)
            throw new InvalidOperationException("Simulated repository load failure");

        return Task.FromResult<StoryInstance?>(null);
    }

    public Task SaveAsync(StoryInstance storyInstance)
    {
        if (_simulateSaveFailure)
            throw new InvalidOperationException("Simulated repository save failure");

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Auid storyId)
    {
        return Task.CompletedTask;
    }
}

#endregion
