using System.Text.Json;
using DreamTravel.Flows.SampleOrderWorkflow;
using DreamTravel.SQLite;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.AUID;
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
        var persisted = await freshRepo.FindById(instance.TaleId, CancellationToken.None);
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
        var completedPersisted = await freshRepo2.FindById(instance.TaleId, CancellationToken.None);
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
        var persisted = await freshRepo.FindById(storyId, CancellationToken.None);
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
        var persisted = await freshRepo.FindById(storyId, CancellationToken.None);
        persisted!.Status.Should().Be(TaleStatus.Failed);
    }

    [Test]
    public async Task Resume_1xHistory_FailsWithDescriptiveError()
    {
        // Arrange
        const string persistedBy1x = """
            {"TaleId":"STR_20261103112154_050288","HandlerTypeName":"OrderProcessingTale","IdempotencyKey":null,"CurrentChapter":{"ChapterId":"RequestCustomerDetailsChapter","StartedAt":"2026-11-03T11:21:54.877769Z","FinishedAt":null,"Status":2,"RequiredData":[{"Name":"Name","Type":"String","IsComplex":false,"Children":[]},{"Name":"Email","Type":"String","IsComplex":false,"Children":[]},{"Name":"Address","Type":"String","IsComplex":false,"Children":[]}],"ProvidedData":null,"Error":null},"History":[{"ChapterId":"OrderValidationChapter","StartedAt":"2026-11-03T11:21:54.831843Z","FinishedAt":"2026-11-03T11:21:54.832482Z","Status":3,"RequiredData":[],"ProvidedData":null,"Error":null},{"ChapterId":"RequestCustomerDetailsChapter","StartedAt":"2026-11-03T11:21:54.877769Z","FinishedAt":null,"Status":2,"RequiredData":[{"Name":"Name","Type":"String","IsComplex":false,"Children":[]},{"Name":"Email","Type":"String","IsComplex":false,"Children":[]},{"Name":"Address","Type":"String","IsComplex":false,"Children":[]}],"ProvidedData":null,"Error":null}],"Status":2,"CreatedAt":"2026-11-03T11:21:54.828295Z","LastUpdatedAt":"2026-11-03T11:21:54.881759Z","Context":"{\u0022ValidationPassed\u0022:true,\u0022CustomerName\u0022:\u0022\u0022,\u0022CustomerEmail\u0022:\u0022\u0022,\u0022CustomerAddress\u0022:\u0022\u0022,\u0022PaymentProcessed\u0022:false,\u0022TransactionId\u0022:\u0022\u0022,\u0022Input\u0022:{\u0022OrderId\u0022:\u0022ORD-1X\u0022,\u0022Amount\u0022:42},\u0022Output\u0022:{\u0022OrderId\u0022:\u0022\u0022,\u0022TransactionId\u0022:\u0022\u0022,\u0022CustomerName\u0022:\u0022\u0022,\u0022ConfirmationSent\u0022:false},\u0022TaleInstanceId\u0022:\u0022STR_20261103112154_050288\u0022,\u0022CurrentChapterId\u0022:\u0022RequestCustomerDetailsChapter\u0022}"}
            """;
        using var json = JsonDocument.Parse(persistedBy1x);
        var taleId = Auid.Parse(json.RootElement.GetProperty("TaleId").GetString()!);
        using var sp = BuildProvider();
        var repository = sp.GetRequiredService<ITaleRepository>();
        await using (var connection = new SqliteConnection($"Data Source={_testDbPath}"))
        {
            await connection.OpenAsync();
            var insert = connection.CreateCommand();
            insert.CommandText = @"
                INSERT INTO TaleInstances (TaleId, HandlerTypeName, Status, Context, CreatedAt, LastUpdatedAt, History, CurrentChapter)
                VALUES (@TaleId, @Handler, @Status, @Context, @CreatedAt, @LastUpdatedAt, @History, @CurrentChapter)";
            insert.Parameters.AddWithValue("@TaleId", taleId.ToString());
            insert.Parameters.AddWithValue("@Handler", json.RootElement.GetProperty("HandlerTypeName").GetString());
            insert.Parameters.AddWithValue("@Status", json.RootElement.GetProperty("Status").GetInt32());
            insert.Parameters.AddWithValue("@Context", json.RootElement.GetProperty("Context").GetString());
            insert.Parameters.AddWithValue("@CreatedAt", json.RootElement.GetProperty("CreatedAt").GetString());
            insert.Parameters.AddWithValue("@LastUpdatedAt", json.RootElement.GetProperty("LastUpdatedAt").GetString());
            insert.Parameters.AddWithValue("@History", json.RootElement.GetProperty("History").GetRawText());
            insert.Parameters.AddWithValue("@CurrentChapter", json.RootElement.GetProperty("CurrentChapter").GetRawText());
            await insert.ExecuteNonQueryAsync();
        }

        var manager = sp.GetRequiredService<TaleManager>();
        var userInput = JsonSerializer.SerializeToElement(new { Name = "Jan", Address = "Wadowice" });

        // Act
        var stored = await repository.FindById(taleId, CancellationToken.None);
        var resume = await manager.ResumeStory<
            SampleOrderWorkflowTale, SampleOrderInput, SampleOrderContext, SampleOrderResult>(taleId, userInput);

        // Assert
        new[] { (int)TaleStatus.Created, (int)TaleStatus.Running, (int)TaleStatus.WaitingForInput, (int)TaleStatus.Completed, (int)TaleStatus.Failed, (int)TaleStatus.Cancelled }
            .Should().Equal(0, 1, 2, 3, 4, 5);
        stored!.Status.Should().Be(TaleStatus.WaitingForInput);
        stored.Version.Should().Be(0);
        stored.History.Should().HaveCount(2).And.AllSatisfy(h => h.StepIndex.Should().BeNull());
        stored.CurrentChapter!.ChapterId.Should().Be("RequestCustomerDetailsChapter");
        resume.IsFailure.Should().BeTrue();
        resume.Error!.Message.Should().Contain("created by SolTechnology.Core.Tale 1.x").And.Contain("restart the tale");
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

