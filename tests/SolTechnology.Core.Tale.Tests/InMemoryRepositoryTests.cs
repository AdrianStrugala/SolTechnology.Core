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
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);

        // Act
        await _repository.SaveAsync(taleInstance);

        // Assert
        var retrieved = await _repository.FindById(taleId);
        retrieved.Should().NotBeNull();
        retrieved!.TaleId.Should().Be(taleId);
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
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        await _repository.SaveAsync(taleInstance);

        // Act - Update the tale
        taleInstance.Status = TaleStatus.Completed;
        await _repository.SaveAsync(taleInstance);

        // Assert
        var retrieved = await _repository.FindById(taleId);
        retrieved!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task Repository_ShouldDeleteTale()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        await _repository.SaveAsync(taleInstance);

        // Act
        await _repository.DeleteAsync(taleId);

        // Assert
        var retrieved = await _repository.FindById(taleId);
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
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        await _repository.SaveAsync(taleInstance);

        // Act
        var retrieved1 = await _repository.FindById(taleId);
        var retrieved2 = await _repository.FindById(taleId);

        // Assert - Should be different instances
        retrieved1.Should().NotBeSameAs(retrieved2);
    }

    [Test]
    public async Task Repository_ShouldNotAffectStoredData_WhenModifyingRetrievedInstance()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        taleInstance.Status = TaleStatus.Running;
        await _repository.SaveAsync(taleInstance);

        // Act - Modify the retrieved instance
        var retrieved = await _repository.FindById(taleId);
        retrieved!.Status = TaleStatus.Completed;

        // Assert - Original should remain unchanged
        var original = await _repository.FindById(taleId);
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
                var taleId = Auid.New("TST");
                var tale = CreateTestTaleInstance(taleId);
                await _repository.SaveAsync(tale);
                return taleId;
            }));
        }

        var taleIds = await Task.WhenAll(tasks);

        // Assert - All stories should be saved
        foreach (var taleId in taleIds)
        {
            var retrieved = await _repository.FindById(taleId);
            retrieved.Should().NotBeNull();
            retrieved!.TaleId.Should().Be(taleId);
        }
    }

    [Test]
    public async Task Repository_ShouldHandleConcurrentReads()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        await _repository.SaveAsync(taleInstance);

        // Act - Read the same tale 100 times concurrently
        var tasks = Enumerable.Range(1, 100).Select(_ =>
            Task.Run(async () => await _repository.FindById(taleId))
        ).ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - All reads should succeed
        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r!.TaleId.Should().Be(taleId);
        });
    }

    [Test]
    public async Task Repository_ShouldHandleConcurrentUpdates()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        taleInstance.Status = TaleStatus.Running;
        await _repository.SaveAsync(taleInstance);

        // Act - Update the same tale concurrently
        var tasks = Enumerable.Range(1, 10).Select(i =>
            Task.Run(async () =>
            {
                var tale = await _repository.FindById(taleId);
                if (tale != null)
                {
                    tale.History.Add(new ChapterInfo
                    {
                        ChapterId = $"Chapter{i}",
                        StartedAt = DateTime.UtcNow,
                        Status = TaleStatus.Running
                    });
                    await _repository.SaveAsync(tale);
                }
            })
        ).ToList();

        await Task.WhenAll(tasks);

        // Assert - Final state should be consistent
        var finalTale = await _repository.FindById(taleId);
        finalTale.Should().NotBeNull();
        finalTale!.TaleId.Should().Be(taleId);
    }

    [Test]
    public async Task Repository_ShouldPreserveChapterHistory()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        taleInstance.History.Add(new ChapterInfo
        {
            ChapterId = "Chapter1",
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            FinishedAt = DateTime.UtcNow.AddMinutes(-4),
            Status = TaleStatus.Completed
        });
        taleInstance.History.Add(new ChapterInfo
        {
            ChapterId = "Chapter2",
            StartedAt = DateTime.UtcNow.AddMinutes(-3),
            FinishedAt = DateTime.UtcNow.AddMinutes(-2),
            Status = TaleStatus.Completed
        });

        // Act
        await _repository.SaveAsync(taleInstance);

        // Assert
        var retrieved = await _repository.FindById(taleId);
        retrieved!.History.Should().HaveCount(2);
        retrieved.History[0].ChapterId.Should().Be("Chapter1");
        retrieved.History[1].ChapterId.Should().Be("Chapter2");
    }

    [Test]
    public async Task Repository_ShouldPreserveCurrentChapter()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        taleInstance.CurrentChapter = new ChapterInfo
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
        await _repository.SaveAsync(taleInstance);

        // Assert
        var retrieved = await _repository.FindById(taleId);
        retrieved!.CurrentChapter.Should().NotBeNull();
        retrieved.CurrentChapter!.ChapterId.Should().Be("WaitingChapter");
        retrieved.CurrentChapter.Status.Should().Be(TaleStatus.WaitingForInput);
        retrieved.CurrentChapter.RequiredData.Should().HaveCount(1);
    }

    [Test]
    public async Task Repository_ShouldHandleMultipleStories()
    {
        // Arrange & Act
        var taleIds = new List<(Auid Id, TaleStatus Status)>();
        for (int i = 1; i <= 10; i++)
        {
            var taleId = Auid.New("TST");
            var expectedStatus = i % 2 == 0 ? TaleStatus.Completed : TaleStatus.Running;
            var tale = CreateTestTaleInstance(taleId);
            tale.Status = expectedStatus;
            await _repository.SaveAsync(tale);
            taleIds.Add((taleId, expectedStatus));
        }

        // Assert - Verify all stories were saved correctly
        foreach (var (id, expectedStatus) in taleIds)
        {
            var retrieved = await _repository.FindById(id);
            retrieved.Should().NotBeNull();
            retrieved!.Status.Should().Be(expectedStatus);
        }
    }

    private TaleInstance CreateTestTaleInstance(Auid? taleId = null)
    {
        return new TaleInstance
        {
            TaleId = taleId ?? Auid.New("TST"),
            HandlerTypeName = "TestTale",
            Status = TaleStatus.Running,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = new List<ChapterInfo>()
        };
    }
}
