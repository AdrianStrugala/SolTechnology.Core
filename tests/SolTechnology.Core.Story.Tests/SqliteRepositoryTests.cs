using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Tests for SQLite persistence layer.
/// Verifies CRUD operations, thread safety, and data integrity.
/// </summary>
[TestFixture]
public class SqliteRepositoryTests
{
    private string _testDbPath = null!;
    private SqliteStoryRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        // Create unique test database for each test
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_stories_{Guid.NewGuid()}.db");
        _repository = new SqliteStoryRepository(_testDbPath);
    }

    [TearDown]
    public void TearDown()
    {
        // Force garbage collection to close any open connections
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Clean up test database
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch (IOException)
            {
                // File might still be locked - ignore for test cleanup
            }
        }
    }

    [Test]
    public async Task SaveAsync_ShouldPersistStoryInstance()
    {
        // Arrange
        var storyInstance = new StoryInstance
        {
            StoryId = "test-story-1",
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{\"value\":42}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = new List<ChapterInfo>
            {
                new() { ChapterId = "Chapter1", StartedAt = DateTime.UtcNow, FinishedAt = DateTime.UtcNow }
            }
        };

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("test-story-1");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.StoryId, Is.EqualTo("test-story-1"));
        Assert.That(retrieved.HandlerTypeName, Is.EqualTo("TestHandler"));
        Assert.That(retrieved.Status, Is.EqualTo(StoryStatus.Running));
        Assert.That(retrieved.Context, Is.EqualTo("{\"value\":42}"));
        Assert.That(retrieved.History, Has.Count.EqualTo(1));
        Assert.That(retrieved.History[0].ChapterId, Is.EqualTo("Chapter1"));
    }

    [Test]
    public async Task FindById_ShouldReturnNull_WhenStoryDoesNotExist()
    {
        // Act
        var result = await _repository.FindById("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task SaveAsync_ShouldUpdateExistingStory()
    {
        // Arrange
        var storyInstance = new StoryInstance
        {
            StoryId = "update-test",
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{\"value\":1}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(storyInstance);

        // Act - Update the same story
        storyInstance.Status = StoryStatus.Completed;
        storyInstance.Context = "{\"value\":2}";
        storyInstance.LastUpdatedAt = DateTime.UtcNow.AddMinutes(5);

        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("update-test");
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(StoryStatus.Completed);
        retrieved.Context.Should().Be("{\"value\":2}");
        retrieved.CreatedAt.Should().BeCloseTo(storyInstance.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveStory()
    {
        // Arrange
        var storyInstance = new StoryInstance
        {
            StoryId = "delete-test",
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(storyInstance);

        // Act
        await _repository.DeleteAsync("delete-test");

        // Assert
        var result = await _repository.FindById("delete-test");
        result.Should().BeNull();
    }

    [Test]
    public async Task SaveAsync_ShouldHandleNullHistory()
    {
        // Arrange
        var storyInstance = new StoryInstance
        {
            StoryId = "null-history-test",
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = null!
        };

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("null-history-test");
        retrieved.Should().NotBeNull();
        retrieved!.History.Should().NotBeNull();
        retrieved.History.Should().BeEmpty();
    }

    [Test]
    public async Task SaveAsync_ShouldHandleEmptyHistory()
    {
        // Arrange
        var storyInstance = new StoryInstance
        {
            StoryId = "empty-history-test",
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = new List<ChapterInfo>()
        };

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("empty-history-test");
        retrieved.Should().NotBeNull();
        retrieved!.History.Should().NotBeNull();
        retrieved.History.Should().BeEmpty();
    }

    [Test]
    public async Task SaveAsync_ShouldHandleCurrentChapter()
    {
        // Arrange
        var currentChapter = new ChapterInfo
        {
            ChapterId = "CurrentChapter",
            StartedAt = DateTime.UtcNow,
            FinishedAt = null
        };

        var storyInstance = new StoryInstance
        {
            StoryId = "current-chapter-test",
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.WaitingForInput,
            Context = "{}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            CurrentChapter = currentChapter
        };

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("current-chapter-test");
        retrieved.Should().NotBeNull();
        retrieved!.CurrentChapter.Should().NotBeNull();
        retrieved.CurrentChapter!.ChapterId.Should().Be("CurrentChapter");
        retrieved.CurrentChapter.StartedAt.Should().BeCloseTo(currentChapter.StartedAt, TimeSpan.FromSeconds(1));
        retrieved.CurrentChapter.FinishedAt.Should().BeNull();
    }

    [Test]
    public async Task SaveAsync_ShouldHandleMultipleHistoryEntries()
    {
        // Arrange
        var storyInstance = new StoryInstance
        {
            StoryId = "multi-history-test",
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = new List<ChapterInfo>
            {
                new() { ChapterId = "Chapter1", StartedAt = DateTime.UtcNow, FinishedAt = DateTime.UtcNow.AddSeconds(1) },
                new() { ChapterId = "Chapter2", StartedAt = DateTime.UtcNow.AddSeconds(2), FinishedAt = DateTime.UtcNow.AddSeconds(3) },
                new() { ChapterId = "Chapter3", StartedAt = DateTime.UtcNow.AddSeconds(4), FinishedAt = DateTime.UtcNow.AddSeconds(5) }
            }
        };

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("multi-history-test");
        retrieved.Should().NotBeNull();
        retrieved!.History.Should().HaveCount(3);
        retrieved.History.Select(h => h.ChapterId).Should().Equal("Chapter1", "Chapter2", "Chapter3");
    }

    [Test]
    public async Task SaveAsync_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var storyCount = 50;

        // Act - Concurrent writes
        for (int i = 0; i < storyCount; i++)
        {
            var index = i; // Create local copy to avoid closure bug
            var storyId = $"concurrent-{index}";
            var task = Task.Run(async () =>
            {
                var instance = new StoryInstance
                {
                    StoryId = storyId,
                    HandlerTypeName = "ConcurrentHandler",
                    Status = StoryStatus.Running,
                    Context = $"{{\"index\":{index}}}",
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };
                await _repository.SaveAsync(instance);
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert - All stories should be persisted
        for (int i = 0; i < storyCount; i++)
        {
            var retrieved = await _repository.FindById($"concurrent-{i}");
            retrieved.Should().NotBeNull();
            retrieved!.Context.Should().Contain($"\"index\":{i}");
        }
    }

    [Test]
    public async Task DeleteAsync_ShouldBeIdempotent()
    {
        // Arrange
        var storyInstance = new StoryInstance
        {
            StoryId = "idempotent-delete-test",
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(storyInstance);

        // Act - Delete twice
        await _repository.DeleteAsync("idempotent-delete-test");
        await _repository.DeleteAsync("idempotent-delete-test"); // Should not throw

        // Assert
        var result = await _repository.FindById("idempotent-delete-test");
        result.Should().BeNull();
    }

    [Test]
    public async Task Repository_ShouldPersistAcrossInstances()
    {
        // Arrange
        var storyInstance = new StoryInstance
        {
            StoryId = "persistence-test",
            HandlerTypeName = "TestHandler",
            Status = StoryStatus.Running,
            Context = "{\"persisted\":true}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(storyInstance);

        // Act - Create new repository instance pointing to same database
        var newRepository = new SqliteStoryRepository(_testDbPath);
        var retrieved = await newRepository.FindById("persistence-test");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Context.Should().Be("{\"persisted\":true}");
    }
}
