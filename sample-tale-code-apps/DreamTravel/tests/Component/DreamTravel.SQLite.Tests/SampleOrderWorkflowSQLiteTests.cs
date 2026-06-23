using System.Text.Json;
using DreamTravel.Flows.SampleOrderWorkflow;
using DreamTravel.SQLite;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Story;
using SolTechnology.Core.Story.Builder;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Orchestration;
using SolTechnology.Core.Story.Persistence;

namespace DreamTravel.SQLite.Tests;

/// <summary>
/// End-to-end SQLite persistence tests driving the real <see cref="SampleOrderWorkflowStory"/>
/// through <see cref="StoryManager"/>. Proves the full engine → SQLite SaveAsync at the pause
/// boundary → FindById + context deserialize on resume → complete path, across repository instances.
/// Complement to the in-memory, API-level <c>SampleOrderWorkflowTests</c> in <c>tests/Component</c>.
/// </summary>
[TestFixture]
public class SampleOrderWorkflowSQLiteTests
{
    private string _testDbPath = null!;

    [SetUp]
    public void SetUp()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"workflow_sqlite_{Guid.NewGuid()}.db");
    }

    [TearDown]
    public void TearDown()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (File.Exists(_testDbPath)) try { File.Delete(_testDbPath); } catch { }
    }

    [Test]
    public async Task HappyPath_FullCycle_WithCrossInstanceDurability()
    {
        // Arrange
        using var sp = BuildProvider();
        var manager = sp.GetRequiredService<StoryManager>();

        // Act — Start: story pauses at the interactive CustomerDetailsChapter
        var startResult = await manager.StartStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            new SampleOrderInput { OrderId = "2137", Quantity = 17 });

        // Assert — paused
        startResult.IsSuccess.Should().BeTrue();
        var instance = startResult.Data!;
        instance.Status.Should().Be(StoryStatus.WaitingForInput);
        instance.StoryId.Should().NotBe(Auid.Empty);

        // Verify cross-instance durability: fresh repo reads the paused state
        var freshRepo = new SQLiteStoryRepository($"Data Source={_testDbPath}");
        var persisted = await freshRepo.FindById(instance.StoryId);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(StoryStatus.WaitingForInput);

        // Act — Resume with valid customer input
        var userInput = JsonSerializer.SerializeToElement(new { Name = "Adus", Address = "yes" });
        var resumeResult = await manager.ResumeStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            instance.StoryId, userInput);

        // Assert — completed
        resumeResult.IsSuccess.Should().BeTrue();
        var completed = resumeResult.Data!;
        completed.Status.Should().Be(StoryStatus.Completed);
        completed.History.Should().NotBeEmpty();

        // Verify completion persisted across instances
        var freshRepo2 = new SQLiteStoryRepository($"Data Source={_testDbPath}");
        var completedPersisted = await freshRepo2.FindById(instance.StoryId);
        completedPersisted!.Status.Should().Be(StoryStatus.Completed);
    }

    [Test]
    public async Task InvalidCustomerInput_StoryStaysPaused()
    {
        // Arrange
        using var sp = BuildProvider();
        var manager = sp.GetRequiredService<StoryManager>();

        var startResult = await manager.StartStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            new SampleOrderInput { OrderId = "3000", Quantity = 5 });

        startResult.IsSuccess.Should().BeTrue();
        var storyId = startResult.Data!.StoryId;

        // Act — Resume with empty Name/Address (CustomerDetailsChapter rejects it)
        var invalidInput = JsonSerializer.SerializeToElement(new { Name = "", Address = "" });
        var resumeResult = await manager.ResumeStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            storyId, invalidInput);

        // Assert — story stays paused (validation failure is retryable, not terminal)
        resumeResult.IsSuccess.Should().BeTrue();
        resumeResult.Data!.Status.Should().Be(StoryStatus.WaitingForInput);

        // Verify persisted state is still WaitingForInput
        var freshRepo = new SQLiteStoryRepository($"Data Source={_testDbPath}");
        var persisted = await freshRepo.FindById(storyId);
        persisted!.Status.Should().Be(StoryStatus.WaitingForInput);

        // Act — Retry with valid input should now succeed
        var validInput = JsonSerializer.SerializeToElement(new { Name = "Adus", Address = "yes" });
        var retryResult = await manager.ResumeStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            storyId, validInput);

        // Assert — story completes after valid retry
        retryResult.IsSuccess.Should().BeTrue();
        retryResult.Data!.Status.Should().Be(StoryStatus.Completed);
    }

    [Test]
    public async Task BackendChapterFailure_StoryFails_AndIsPersisted()
    {
        // Arrange — Quantity < 0 triggers BackendProcessingChapter failure
        using var sp = BuildProvider();
        var manager = sp.GetRequiredService<StoryManager>();

        var startResult = await manager.StartStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            new SampleOrderInput { OrderId = "FAIL-001", Quantity = -1 });

        startResult.IsSuccess.Should().BeTrue();
        var storyId = startResult.Data!.StoryId;

        // Act — Resume with valid customer input; backend chapter will fail
        var validInput = JsonSerializer.SerializeToElement(new { Name = "Tester", Address = "Home" });
        var resumeResult = await manager.ResumeStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            storyId, validInput);

        // Assert — engine returns failure (automated chapter error bubbles as Result.Fail)
        resumeResult.IsSuccess.Should().BeFalse();

        // Verify failed state is persisted
        var freshRepo = new SQLiteStoryRepository($"Data Source={_testDbPath}");
        var persisted = await freshRepo.FindById(storyId);
        persisted!.Status.Should().Be(StoryStatus.Failed);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton(new SQLiteStoryRepositoryOptions { ConnectionString = $"Data Source={_testDbPath}" });
        services.RegisterStories(assemblies: typeof(SampleOrderWorkflowStory).Assembly)
            .UseStoryRepository<SQLiteStoryRepository>();
        return services.BuildServiceProvider();
    }
}

