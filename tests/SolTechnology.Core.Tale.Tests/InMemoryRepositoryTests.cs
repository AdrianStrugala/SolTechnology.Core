using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>
/// Tests for InMemoryTaleRepository.
/// Verifies CRUD operations, thread-safety, and data immutability.
/// </summary>
[TestFixture]
public class InMemoryRepositoryTests
{
    private InMemoryTaleRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryTaleRepository();
    }

    [Test]
    public async Task Repository_ShouldSaveTaleInstance()
    {
        // Arrange
        var storyId = Auid.New("TST");
        var storyInstance = CreateTestTaleInstance(storyId);

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById(storyId);
        retrieved.Should().NotBeNull();
        retrieved!.TaleId.Should().Be(storyId);
    }

    [Test]
    public async Task Repository_ShouldReturnNull_WhenTaleNotFound()
    {
        // Act
        var result = await _repository.FindById(Auid.New("TST"));

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task Repository_ShouldUpdateExistingTale()
    {
        // Arrange
        var storyId = Auid.New("TST");
        var storyInstance = CreateTestTaleInstance(storyId);
        await _repository.SaveAsync(storyInstance);

        // Act - Update the story
        storyInstance.Status = TaleStatus.Completed;
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById(storyId);
        retrieved!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task Repository_ShouldDeleteTale()
    {
        // Arrange
        var storyId = Auid.New("TST");
        var storyInstance = CreateTestTaleInstance(storyId);
        await _repository.SaveAsync(storyInstance);

        // Act
        await _repository.DeleteAsync(storyId);

        // Assert
        var retrieved = await _repository.FindById(storyId);
        retrieved.Should().BeNull();
    }

    [Test]
    public async Task Repository_Delete_ShouldNotThrow_WhenTaleDoesNotExist()
    {
        // Act & Assert
        var act = async () => await _repository.DeleteAsync(Auid.New("TST"));
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Repository_ShouldReturnClonedInstance_ToPreventMutation()
    {
        // Arrange
        var storyId = Auid.New("TST");
        var storyInstance = CreateTestTaleInstance(storyId);
        await _repository.SaveAsync(storyInstance);

        // Act
        var retrieved1 = await _repository.FindById(storyId);
        var retrieved2 = await _repository.FindById(storyId);

        // Assert - Should be different instances
        retrieved1.Should().NotBeSameAs(retrieved2);
    }

    [Test]
    public async Task Repository_ShouldNotAffectStoredData_WhenModifyingRetrievedInstance()
    {
        // Arrange
        var storyId = Auid.New("TST");
        var storyInstance = CreateTestTaleInstance(storyId);
        storyInstance.Status = TaleStatus.Running;
        await _repository.SaveAsync(storyInstance);

        // Act - Modify the retrieved instance
        var retrieved = await _repository.FindById(storyId);
        retrieved!.Status = TaleStatus.Completed;

        // Assert - Original should remain unchanged
        var original = await _repository.FindById(storyId);
        original!.Status.Should().Be(TaleStatus.Running);
    }

    [Test]
    public async Task Repository_ShouldHandleConcurrentWrites()
    {
        // Arrange
        var tasks = new List<Task<Auid>>();
        var count = 100;

        // Act - Write 100 stories concurrently
        for (int i = 0; i < count; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var storyId = Auid.New("TST");
                var story = CreateTestTaleInstance(storyId);
                await _repository.SaveAsync(story);
                return storyId;
            }));
        }

        var storyIds = await Task.WhenAll(tasks);

        // Assert - All stories should be saved
        foreach (var storyId in storyIds)
        {
            var retrieved = await _repository.FindById(storyId);
            retrieved.Should().NotBeNull();
            retrieved!.TaleId.Should().Be(storyId);
        }
    }

    [Test]
    public async Task Repository_ShouldHandleConcurrentReads()
    {
        // Arrange
        var storyId = Auid.New("TST");
        var storyInstance = CreateTestTaleInstance(storyId);
        await _repository.SaveAsync(storyInstance);

        // Act - Read the same story 100 times concurrently
        var tasks = Enumerable.Range(1, 100).Select(_ =>
            Task.Run(async () => await _repository.FindById(storyId))
        ).ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - All reads should succeed
        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r!.TaleId.Should().Be(storyId);
        });
    }

    [Test]
    public async Task Repository_ShouldHandleConcurrentUpdates()
    {
        // Arrange
        var storyId = Auid.New("TST");
        var storyInstance = CreateTestTaleInstance(storyId);
        storyInstance.Status = TaleStatus.Running;
        await _repository.SaveAsync(storyInstance);

        // Act - Update the same story concurrently
        var tasks = Enumerable.Range(1, 10).Select(i =>
            Task.Run(async () =>
            {
                var story = await _repository.FindById(storyId);
                if (story != null)
                {
                    story.History.Add(new ChapterInfo
                    {
                        ChapterId = $"Chapter{i}",
                        StartedAt = DateTime.UtcNow,
                        Status = TaleStatus.Running
                    });
                    await _repository.SaveAsync(story);
                }
            })
        ).ToList();

        await Task.WhenAll(tasks);

        // Assert - Final state should be consistent
        var finalTale = await _repository.FindById(storyId);
        finalTale.Should().NotBeNull();
        finalTale!.TaleId.Should().Be(storyId);
    }

    [Test]
    public async Task Repository_ShouldPreserveChapterHistory()
    {
        // Arrange
        var storyId = Auid.New("TST");
        var storyInstance = CreateTestTaleInstance(storyId);
        storyInstance.History.Add(new ChapterInfo
        {
            ChapterId = "Chapter1",
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            FinishedAt = DateTime.UtcNow.AddMinutes(-4),
            Status = TaleStatus.Completed
        });
        storyInstance.History.Add(new ChapterInfo
        {
            ChapterId = "Chapter2",
            StartedAt = DateTime.UtcNow.AddMinutes(-3),
            FinishedAt = DateTime.UtcNow.AddMinutes(-2),
            Status = TaleStatus.Completed
        });

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById(storyId);
        retrieved!.History.Should().HaveCount(2);
        retrieved.History[0].ChapterId.Should().Be("Chapter1");
        retrieved.History[1].ChapterId.Should().Be("Chapter2");
    }

    [Test]
    public async Task Repository_ShouldPreserveCurrentChapter()
    {
        // Arrange
        var storyId = Auid.New("TST");
        var storyInstance = CreateTestTaleInstance(storyId);
        storyInstance.CurrentChapter = new ChapterInfo
        {
            ChapterId = "WaitingChapter",
            StartedAt = DateTime.UtcNow,
            Status = TaleStatus.WaitingForInput,
            RequiredData = new List<DataField>
            {
                new DataField { Name = "UserName", Type = "String", IsComplex = false }
            }
        };

        // Act
        await _repository.SaveAsync(storyInstance);

        // Assert
        var retrieved = await _repository.FindById(storyId);
        retrieved!.CurrentChapter.Should().NotBeNull();
        retrieved.CurrentChapter!.ChapterId.Should().Be("WaitingChapter");
        retrieved.CurrentChapter.Status.Should().Be(TaleStatus.WaitingForInput);
        retrieved.CurrentChapter.RequiredData.Should().HaveCount(1);
    }

    [Test]
    public async Task Repository_ShouldHandleMultipleStories()
    {
        // Arrange & Act
        var storyIds = new List<(Auid Id, TaleStatus Status)>();
        for (int i = 1; i <= 10; i++)
        {
            var storyId = Auid.New("TST");
            var expectedStatus = i % 2 == 0 ? TaleStatus.Completed : TaleStatus.Running;
            var story = CreateTestTaleInstance(storyId);
            story.Status = expectedStatus;
            await _repository.SaveAsync(story);
            storyIds.Add((storyId, expectedStatus));
        }

        // Assert - Verify all stories were saved correctly
        foreach (var (id, expectedStatus) in storyIds)
        {
            var retrieved = await _repository.FindById(id);
            retrieved.Should().NotBeNull();
            retrieved!.Status.Should().Be(expectedStatus);
        }
    }

    private TaleInstance CreateTestTaleInstance(Auid? storyId = null)
    {
        return new TaleInstance
        {
            TaleId = storyId ?? Auid.New("TST"),
            HandlerTypeName = "TestTale",
            Status = TaleStatus.Running,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = new List<ChapterInfo>()
        };
    }
}
