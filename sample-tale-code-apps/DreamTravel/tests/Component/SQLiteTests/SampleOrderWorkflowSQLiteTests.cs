using System.Text.Json;
using DreamTravel.Flows.SampleOrderWorkflow;
using DreamTravel.SQLite;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale.Builder;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
using SolTechnology.Core.Tale.Persistence;

namespace DreamTravel.FunctionalTests.SQLiteTests;

/// <summary>
/// End-to-end SQLite persistence tests driving the real <see cref="SampleOrderWorkflowTale"/>
/// through <see cref="TaleManager"/>. Proves the full engine → SQLite → pause/resume path,
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
        SqliteConnection.ClearAllPools();
        if (File.Exists(_testDbPath)) try { File.Delete(_testDbPath); } catch { }
    }

    [Test]
    public async Task HappyPath_FullCycle_WithCrossInstanceDurability()
    {
        using var sp = BuildProvider();
        var manager = sp.GetRequiredService<TaleManager>();

        var startResult = await manager.StartStory<
            SampleOrderWorkflowTale, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            new SampleOrderInput { OrderId = "2137", Quantity = 17 });

        startResult.IsSuccess.Should().BeTrue();
        var instance = startResult.Data!;
        instance.Status.Should().Be(TaleStatus.WaitingForInput);
        instance.TaleId.Should().NotBe(Auid.Empty);

        // Cross-instance durability: fresh repo reads the paused state
        var freshRepo = new SQLiteTaleRepository($"Data Source={_testDbPath}");
        var persisted = await freshRepo.FindById(instance.TaleId);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(TaleStatus.WaitingForInput);

        // Resume with valid customer input
        var userInput = JsonSerializer.SerializeToElement(new { Name = "Adus", Address = "yes" });
        var resumeResult = await manager.ResumeStory<
            SampleOrderWorkflowTale, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            instance.TaleId, userInput);

        resumeResult.IsSuccess.Should().BeTrue();
        var completed = resumeResult.Data!;
        completed.Status.Should().Be(TaleStatus.Completed);
        completed.History.Should().NotBeEmpty();

        // Completion persisted across instances
        var freshRepo2 = new SQLiteTaleRepository($"Data Source={_testDbPath}");
        var completedPersisted = await freshRepo2.FindById(instance.TaleId);
        completedPersisted!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task InvalidCustomerInput_TaleStaysPaused_ThenRetrySucceeds()
    {
        using var sp = BuildProvider();
        var manager = sp.GetRequiredService<TaleManager>();

        var startResult = await manager.StartStory<
            SampleOrderWorkflowTale, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            new SampleOrderInput { OrderId = "3000", Quantity = 5 });

        startResult.IsSuccess.Should().BeTrue();
        var storyId = startResult.Data!.TaleId;

        // Resume with empty Name/Address — validation rejects, story stays paused
        var invalidInput = JsonSerializer.SerializeToElement(new { Name = "", Address = "" });
        var resumeResult = await manager.ResumeStory<
            SampleOrderWorkflowTale, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            storyId, invalidInput);

        resumeResult.IsSuccess.Should().BeTrue();
        resumeResult.Data!.Status.Should().Be(TaleStatus.WaitingForInput);

        // Persisted state is still WaitingForInput
        var freshRepo = new SQLiteTaleRepository($"Data Source={_testDbPath}");
        var persisted = await freshRepo.FindById(storyId);
        persisted!.Status.Should().Be(TaleStatus.WaitingForInput);

        // Retry with valid input — story completes
        var validInput = JsonSerializer.SerializeToElement(new { Name = "Adus", Address = "yes" });
        var retryResult = await manager.ResumeStory<
            SampleOrderWorkflowTale, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            storyId, validInput);

        retryResult.IsSuccess.Should().BeTrue();
        retryResult.Data!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task BackendChapterFailure_TaleFails_AndIsPersisted()
    {
        using var sp = BuildProvider();
        var manager = sp.GetRequiredService<TaleManager>();

        var startResult = await manager.StartStory<
            SampleOrderWorkflowTale, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            new SampleOrderInput { OrderId = "FAIL-001", Quantity = -1 });

        startResult.IsSuccess.Should().BeTrue();
        var storyId = startResult.Data!.TaleId;

        // Resume with valid customer input; backend chapter will fail (Quantity < 0)
        var validInput = JsonSerializer.SerializeToElement(new { Name = "Tester", Address = "Home" });
        var resumeResult = await manager.ResumeStory<
            SampleOrderWorkflowTale, SampleOrderInput, SampleOrderContext, SampleOrderResult>(
            storyId, validInput);

        // Automated chapter failure is terminal
        resumeResult.IsSuccess.Should().BeFalse();

        // Failed state is persisted
        var freshRepo = new SQLiteTaleRepository($"Data Source={_testDbPath}");
        var persisted = await freshRepo.FindById(storyId);
        persisted!.Status.Should().Be(TaleStatus.Failed);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton(new SQLiteTaleRepositoryOptions { ConnectionString = $"Data Source={_testDbPath}" });
        services.AddSolTale(assemblies: typeof(SampleOrderWorkflowTale).Assembly)
            .UseTaleRepository<SQLiteTaleRepository>();
        return services.BuildServiceProvider();
    }
}

