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

namespace DreamTravel.FunctionalTests.SQLiteTests;

/// <summary>
/// End-to-end SQLite persistence tests driving the real <see cref="SampleOrderWorkflowStory"/>
/// through <see cref="StoryManager"/>. Proves the full engine → SQLite → pause/resume path,
/// across repository instances. Complement to the in-memory API-level SampleOrderWorkflowTests.
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
        using var sp = BuildProvider();
        var manager = sp.GetRequiredService<StoryManager>();

        var startResult = await manager.StartStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            new SampleOrderInput { OrderId = "2137", Quantity = 17 });

        startResult.IsSuccess.Should().BeTrue();
        var instance = startResult.Data!;
        instance.Status.Should().Be(StoryStatus.WaitingForInput);
        instance.StoryId.Should().NotBe(Auid.Empty);

        // Cross-instance durability: fresh repo reads the paused state
        var freshRepo = new SQLiteStoryRepository($"Data Source={_testDbPath}");
        var persisted = await freshRepo.FindById(instance.StoryId);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(StoryStatus.WaitingForInput);

        // Resume with valid customer input
        var userInput = JsonSerializer.SerializeToElement(new { Name = "Adus", Address = "yes" });
        var resumeResult = await manager.ResumeStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            instance.StoryId, userInput);

        resumeResult.IsSuccess.Should().BeTrue();
        var completed = resumeResult.Data!;
        completed.Status.Should().Be(StoryStatus.Completed);
        completed.History.Should().NotBeEmpty();

        // Completion persisted across instances
        var freshRepo2 = new SQLiteStoryRepository($"Data Source={_testDbPath}");
        var completedPersisted = await freshRepo2.FindById(instance.StoryId);
        completedPersisted!.Status.Should().Be(StoryStatus.Completed);
    }

    [Test]
    public async Task InvalidCustomerInput_StoryStaysPaused_ThenRetrySucceeds()
    {
        using var sp = BuildProvider();
        var manager = sp.GetRequiredService<StoryManager>();

        var startResult = await manager.StartStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            new SampleOrderInput { OrderId = "3000", Quantity = 5 });

        startResult.IsSuccess.Should().BeTrue();
        var storyId = startResult.Data!.StoryId;

        // Resume with empty Name/Address — validation rejects, story stays paused
        var invalidInput = JsonSerializer.SerializeToElement(new { Name = "", Address = "" });
        var resumeResult = await manager.ResumeStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            storyId, invalidInput);

        resumeResult.IsSuccess.Should().BeTrue();
        resumeResult.Data!.Status.Should().Be(StoryStatus.WaitingForInput);

        // Persisted state is still WaitingForInput
        var freshRepo = new SQLiteStoryRepository($"Data Source={_testDbPath}");
        var persisted = await freshRepo.FindById(storyId);
        persisted!.Status.Should().Be(StoryStatus.WaitingForInput);

        // Retry with valid input — story completes
        var validInput = JsonSerializer.SerializeToElement(new { Name = "Adus", Address = "yes" });
        var retryResult = await manager.ResumeStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            storyId, validInput);

        retryResult.IsSuccess.Should().BeTrue();
        retryResult.Data!.Status.Should().Be(StoryStatus.Completed);
    }

    [Test]
    public async Task BackendChapterFailure_StoryFails_AndIsPersisted()
    {
        using var sp = BuildProvider();
        var manager = sp.GetRequiredService<StoryManager>();

        var startResult = await manager.StartStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            new SampleOrderInput { OrderId = "FAIL-001", Quantity = -1 });

        startResult.IsSuccess.Should().BeTrue();
        var storyId = startResult.Data!.StoryId;

        // Resume with valid customer input; backend chapter will fail (Quantity < 0)
        var validInput = JsonSerializer.SerializeToElement(new { Name = "Tester", Address = "Home" });
        var resumeResult = await manager.ResumeStory<
            SampleOrderWorkflowStory, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            storyId, validInput);

        // Automated chapter failure is terminal
        resumeResult.IsSuccess.Should().BeFalse();

        // Failed state is persisted
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

