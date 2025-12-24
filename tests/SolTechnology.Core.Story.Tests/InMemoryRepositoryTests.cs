using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Story.Models;
using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story.Tests;

/// <summary>
/// Tests for InMemoryStoryRepository.
/// Verifies CRUD operations, thread-safety, and data immutability.
/// </summary>
[TestFixture]
public class InMemoryRepositoryTests
{
    private InMemoryStoryRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryStoryRepository();
    }

    [Test]
    public async Task Repository_ShouldSaveStoryInstance()
    {
        // Arrange
        var storyInstance = CreateTestStoryInstance("story-001");

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("story-001");
        retrieved.Should().NotBeNull();
        retrieved!.StoryId.Should().Be("story-001");
    }

    [Test]
    public async Task Repository_ShouldReturnNull_WhenStoryNotFound()
    {
        // Act
        var result = await _repository.FindById("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task Repository_ShouldUpdateExistingStory()
    {
        // Arrange
        var storyInstance = CreateTestStoryInstance("story-002");
        await _repository.SaveAsync(storyInstance);

        // Act - Update the story
        storyInstance.Status = StoryStatus.Completed;
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("story-002");
        retrieved!.Status.Should().Be(StoryStatus.Completed);
    }

    [Test]
    public async Task Repository_ShouldDeleteStory()
    {
        // Arrange
        var storyInstance = CreateTestStoryInstance("story-003");
        await _repository.SaveAsync(storyInstance);

        // Act
        await _repository.DeleteAsync("story-003");

        // Assert
        var retrieved = await _repository.FindById("story-003");
        retrieved.Should().BeNull();
    }

    [Test]
    public async Task Repository_Delete_ShouldNotThrow_WhenStoryDoesNotExist()
    {
        // Act & Assert
        var act = async () => await _repository.DeleteAsync("non-existent");
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Repository_ShouldReturnClonedInstance_ToPreventMutation()
    {
        // Arrange
        var storyInstance = CreateTestStoryInstance("story-004");
        await _repository.SaveAsync(storyInstance);

        // Act
        var retrieved1 = await _repository.FindById("story-004");
        var retrieved2 = await _repository.FindById("story-004");

        // Assert - Should be different instances
        retrieved1.Should().NotBeSameAs(retrieved2);
    }

    [Test]
    public async Task Repository_ShouldNotAffectStoredData_WhenModifyingRetrievedInstance()
    {
        // Arrange
        var storyInstance = CreateTestStoryInstance("story-005");
        storyInstance.Status = StoryStatus.Running;
        await _repository.SaveAsync(storyInstance);

        // Act - Modify the retrieved instance
        var retrieved = await _repository.FindById("story-005");
        retrieved!.Status = StoryStatus.Completed;

        // Assert - Original should remain unchanged
        var original = await _repository.FindById("story-005");
        original!.Status.Should().Be(StoryStatus.Running);
    }

    [Test]
    public async Task Repository_ShouldHandleConcurrentWrites()
    {
        // Arrange
        var tasks = new List<Task>();
        var storyIds = Enumerable.Range(1, 100).Select(i => $"story-{i:D3}").ToList();

        // Act - Write 100 stories concurrently
        foreach (var storyId in storyIds)
        {
            tasks.Add(Task.Run(async () =>
            {
                var story = CreateTestStoryInstance(storyId);
                await _repository.SaveAsync(story);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All stories should be saved
        foreach (var storyId in storyIds)
        {
            var retrieved = await _repository.FindById(storyId);
            retrieved.Should().NotBeNull();
            retrieved!.StoryId.Should().Be(storyId);
        }
    }

    [Test]
    public async Task Repository_ShouldHandleConcurrentReads()
    {
        // Arrange
        var storyInstance = CreateTestStoryInstance("story-concurrent");
        await _repository.SaveAsync(storyInstance);

        // Act - Read the same story 100 times concurrently
        var tasks = Enumerable.Range(1, 100).Select(_ =>
            Task.Run(async () => await _repository.FindById("story-concurrent"))
        ).ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - All reads should succeed
        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r!.StoryId.Should().Be("story-concurrent");
        });
    }

    [Test]
    public async Task Repository_ShouldHandleConcurrentUpdates()
    {
        // Arrange
        var storyInstance = CreateTestStoryInstance("story-update");
        storyInstance.Status = StoryStatus.Running;
        await _repository.SaveAsync(storyInstance);

        // Act - Update the same story concurrently
        var tasks = Enumerable.Range(1, 10).Select(i =>
            Task.Run(async () =>
            {
                var story = await _repository.FindById("story-update");
                if (story != null)
                {
                    story.History.Add(new ChapterInfo
                    {
                        ChapterId = $"Chapter{i}",
                        StartedAt = DateTime.UtcNow,
                        Status = StoryStatus.Running
                    });
                    await _repository.SaveAsync(story);
                }
            })
        ).ToList();

        await Task.WhenAll(tasks);

        // Assert - Final state should be consistent
        var finalStory = await _repository.FindById("story-update");
        finalStory.Should().NotBeNull();
        finalStory!.StoryId.Should().Be("story-update");
    }

    [Test]
    public async Task Repository_ShouldPreserveChapterHistory()
    {
        // Arrange
        var storyInstance = CreateTestStoryInstance("story-history");
        storyInstance.History.Add(new ChapterInfo
        {
            ChapterId = "Chapter1",
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            FinishedAt = DateTime.UtcNow.AddMinutes(-4),
            Status = StoryStatus.Completed
        });
        storyInstance.History.Add(new ChapterInfo
        {
            ChapterId = "Chapter2",
            StartedAt = DateTime.UtcNow.AddMinutes(-3),
            FinishedAt = DateTime.UtcNow.AddMinutes(-2),
            Status = StoryStatus.Completed
        });

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("story-history");
        retrieved!.History.Should().HaveCount(2);
        retrieved.History[0].ChapterId.Should().Be("Chapter1");
        retrieved.History[1].ChapterId.Should().Be("Chapter2");
    }

    [Test]
    public async Task Repository_ShouldPreserveCurrentChapter()
    {
        // Arrange
        var storyInstance = CreateTestStoryInstance("story-current");
        storyInstance.CurrentChapter = new ChapterInfo
        {
            ChapterId = "WaitingChapter",
            StartedAt = DateTime.UtcNow,
            Status = StoryStatus.WaitingForInput,
            RequiredData = new List<DataField>
            {
                new DataField { Name = "UserName", Type = "String", IsComplex = false }
            }
        };

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById("story-current");
        retrieved!.CurrentChapter.Should().NotBeNull();
        retrieved.CurrentChapter!.ChapterId.Should().Be("WaitingChapter");
        retrieved.CurrentChapter.Status.Should().Be(StoryStatus.WaitingForInput);
        retrieved.CurrentChapter.RequiredData.Should().HaveCount(1);
    }

    [Test]
    public async Task Repository_ShouldHandleMultipleStories()
    {
        // Arrange & Act
        for (int i = 1; i <= 10; i++)
        {
            var story = CreateTestStoryInstance($"story-{i}");
            story.Status = i % 2 == 0 ? StoryStatus.Completed : StoryStatus.Running;
            await _repository.SaveAsync(story);
        }

        // Assert - Verify all stories were saved correctly
        for (int i = 1; i <= 10; i++)
        {
            var retrieved = await _repository.FindById($"story-{i}");
            retrieved.Should().NotBeNull();
            retrieved!.Status.Should().Be(i % 2 == 0 ? StoryStatus.Completed : StoryStatus.Running);
        }
    }

    private StoryInstance CreateTestStoryInstance(string storyId)
    {
        return new StoryInstance
        {
            StoryId = storyId,
            HandlerTypeName = "TestStory",
            Status = StoryStatus.Running,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = new List<ChapterInfo>()
        };
    }
}
