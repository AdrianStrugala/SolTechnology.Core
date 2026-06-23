using DreamTravel.SQLite;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SolTechnology.Core.Story.Models;

namespace DreamTravel.FunctionalTests.SQLiteTests;

/// <summary>
/// Tests for the SQLite persistence layer.
/// Verifies CRUD operations, thread safety, cross-instance durability, and data integrity.
/// </summary>
[TestFixture]
public class SQLiteRepositoryTests
{
    private string _testDbPath = null!;
    private SQLiteStoryRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_stories_{Guid.NewGuid()}.db");
        _repository = new SQLiteStoryRepository($"Data Source={_testDbPath}");
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); }
            catch (IOException) { }
        }
    }

    [Test]
    public async Task SaveAsync_ShouldPersistStoryInstance()
    {
        var storyId = Auid.New("TST");
        var storyInstance = new StoryInstance
        {
            StoryId = storyId,
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{\"value\":42}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = [new() { ChapterId = "Chapter1", StartedAt = DateTime.UtcNow, FinishedAt = DateTime.UtcNow }]
        };

        await _repository.SaveAsync(storyInstance);

        var retrieved = await _repository.FindById(storyId);
        retrieved.Should().NotBeNull();
        retrieved!.StoryId.Should().Be(storyId);
        retrieved.HandlerTypeName.Should().Be("TestHandler");
        retrieved.Status.Should().Be(StoryStatus.Running);
        retrieved.Context.Should().Be("{\"value\":42}");
        retrieved.History.Should().HaveCount(1);
        retrieved.History[0].ChapterId.Should().Be("Chapter1");
    }

    [Test]
    public async Task FindById_ShouldReturnNull_WhenStoryDoesNotExist()
    {
        var result = await _repository.FindById(Auid.New("TST"));
        result.Should().BeNull();
    }

    [Test]
    public async Task SaveAsync_ShouldUpdateExistingStory()
    {
        var storyId = Auid.New("TST");
        var storyInstance = new StoryInstance
        {
            StoryId = storyId,
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{\"value\":1}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(storyInstance);

        storyInstance.Status = StoryStatus.Completed;
        storyInstance.Context = "{\"value\":2}";
        storyInstance.LastUpdatedAt = DateTime.UtcNow.AddMinutes(5);
        await _repository.SaveAsync(storyInstance);

        var retrieved = await _repository.FindById(storyId);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(StoryStatus.Completed);
        retrieved.Context.Should().Be("{\"value\":2}");
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveStory()
    {
        var storyId = Auid.New("TST");
        var storyInstance = new StoryInstance
        {
            StoryId = storyId, HandlerTypeName = "TestHandler", Status = StoryStatus.Running,
            Context = "{}", CreatedAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(storyInstance);
        await _repository.DeleteAsync(storyId);

        var result = await _repository.FindById(storyId);
        result.Should().BeNull();
    }

    [Test]
    public async Task SaveAsync_ShouldHandleCurrentChapter()
    {
        var storyId = Auid.New("TST");
        var currentChapter = new ChapterInfo { ChapterId = "CurrentChapter", StartedAt = DateTime.UtcNow };
        var storyInstance = new StoryInstance
        {
            StoryId = storyId, HandlerTypeName = "TestHandler", Status = StoryStatus.WaitingForInput,
            Context = "{}", CreatedAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow,
            CurrentChapter = currentChapter
        };

        await _repository.SaveAsync(storyInstance);

        var retrieved = await _repository.FindById(storyId);
        retrieved!.CurrentChapter.Should().NotBeNull();
        retrieved.CurrentChapter!.ChapterId.Should().Be("CurrentChapter");
    }

    [Test]
    public async Task SaveAsync_ShouldBeThreadSafe()
    {
        var storyIds = Enumerable.Range(0, 50).Select(_ => Auid.New("TST")).ToList();

        var tasks = storyIds.Select((id, i) => Task.Run(async () =>
        {
            var instance = new StoryInstance
            {
                StoryId = id, HandlerTypeName = "ConcurrentHandler", Status = StoryStatus.Running,
                Context = $"{{\"index\":{i}}}", CreatedAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow
            };
            await _repository.SaveAsync(instance);
        }));

        await Task.WhenAll(tasks);

        foreach (var (id, i) in storyIds.Select((id, i) => (id, i)))
        {
            var retrieved = await _repository.FindById(id);
            retrieved.Should().NotBeNull();
            retrieved!.Context.Should().Contain($"\"index\":{i}");
        }
    }

    [Test]
    public async Task Repository_ShouldPersistAcrossInstances()
    {
        var storyId = Auid.New("TST");
        var storyInstance = new StoryInstance
        {
            StoryId = storyId, HandlerTypeName = "TestHandler", Status = StoryStatus.Running,
            Context = "{\"persisted\":true}", CreatedAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(storyInstance);

        var newRepository = new SQLiteStoryRepository($"Data Source={_testDbPath}");
        var retrieved = await newRepository.FindById(storyId);

        retrieved.Should().NotBeNull();
        retrieved!.Context.Should().Be("{\"persisted\":true}");
    }
}

