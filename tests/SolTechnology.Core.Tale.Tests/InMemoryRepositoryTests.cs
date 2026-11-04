using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;
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
        await _repository.Save(taleInstance, CancellationToken.None);

        // Assert
        var retrieved = await _repository.FindById(taleId, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.TaleId.Should().Be(taleId);
    }

    [Test]
    public async Task Repository_ShouldReturnNull_WhenTaleNotFound()
    {
        // Act
        var result = await _repository.FindById(Auid.New("TST"), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task Repository_ShouldUpdateExistingTale()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        await _repository.Save(taleInstance, CancellationToken.None);

        // Act - Update the tale
        taleInstance.Status = TaleStatus.Completed;
        await _repository.Save(taleInstance, CancellationToken.None);

        // Assert
        var retrieved = await _repository.FindById(taleId, CancellationToken.None);
        retrieved!.Status.Should().Be(TaleStatus.Completed);
    }

    [Test]
    public async Task Repository_ShouldDeleteTale()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        await _repository.Save(taleInstance, CancellationToken.None);

        // Act
        await _repository.Delete(taleId, CancellationToken.None);

        // Assert
        var retrieved = await _repository.FindById(taleId, CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Test]
    public async Task Repository_Delete_ShouldNotThrow_WhenTaleDoesNotExist()
    {
        // Act & Assert
        var act = async () => await _repository.Delete(Auid.New("TST"), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Repository_ShouldReturnClonedInstance_ToPreventMutation()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        await _repository.Save(taleInstance, CancellationToken.None);

        // Act
        var retrieved1 = await _repository.FindById(taleId, CancellationToken.None);
        var retrieved2 = await _repository.FindById(taleId, CancellationToken.None);

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
        await _repository.Save(taleInstance, CancellationToken.None);

        // Act - Modify the retrieved instance
        var retrieved = await _repository.FindById(taleId, CancellationToken.None);
        retrieved!.Status = TaleStatus.Completed;

        // Assert - Original should remain unchanged
        var original = await _repository.FindById(taleId, CancellationToken.None);
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
                await _repository.Save(tale, CancellationToken.None);
                return taleId;
            }));
        }

        var taleIds = await Task.WhenAll(tasks);

        // Assert - All stories should be saved
        foreach (var taleId in taleIds)
        {
            var retrieved = await _repository.FindById(taleId, CancellationToken.None);
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
        await _repository.Save(taleInstance, CancellationToken.None);

        // Act - Read the same tale 100 times concurrently
        var tasks = Enumerable.Range(1, 100).Select(_ =>
            Task.Run(async () => await _repository.FindById(taleId, CancellationToken.None))
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
        await _repository.Save(taleInstance, CancellationToken.None);

        // Act - Update the same tale concurrently
        var tasks = Enumerable.Range(1, 10).Select(i =>
            Task.Run(async () =>
            {
                var tale = await _repository.FindById(taleId, CancellationToken.None);
                if (tale != null)
                {
                    tale.History.Add(new ChapterInfo
                    {
                        ChapterId = $"Chapter{i}",
                        StartedAt = DateTime.UtcNow,
                        Status = TaleStatus.Running
                    });
                    await _repository.Save(tale, CancellationToken.None);
                }
            })
        ).ToList();

        await Task.WhenAll(tasks);

        // Assert - Final state should be consistent
        var finalTale = await _repository.FindById(taleId, CancellationToken.None);
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
        await _repository.Save(taleInstance, CancellationToken.None);

        // Assert
        var retrieved = await _repository.FindById(taleId, CancellationToken.None);
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
        await _repository.Save(taleInstance, CancellationToken.None);

        // Assert
        var retrieved = await _repository.FindById(taleId, CancellationToken.None);
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
            await _repository.Save(tale, CancellationToken.None);
            taleIds.Add((taleId, expectedStatus));
        }

        // Assert - Verify all stories were saved correctly
        foreach (var (id, expectedStatus) in taleIds)
        {
            var retrieved = await _repository.FindById(id, CancellationToken.None);
            retrieved.Should().NotBeNull();
            retrieved!.Status.Should().Be(expectedStatus);
        }
    }

    [Test]
    public async Task Save_StaleVersion_ReturnsConflict()
    {
        // Arrange
        var taleId = Auid.New("TST");
        await _repository.Save(CreateTestTaleInstance(taleId), CancellationToken.None);
        var first = (await _repository.FindById(taleId, CancellationToken.None))!;
        var second = (await _repository.FindById(taleId, CancellationToken.None))!;
        first.Status = TaleStatus.Completed;
        second.Status = TaleStatus.Cancelled;
        await _repository.Save(first, CancellationToken.None);

        // Act
        var stale = await _repository.Save(second, CancellationToken.None);
        var duplicate = await _repository.Save(CreateTestTaleInstance(taleId), CancellationToken.None);

        // Assert
        stale.Error.Should().BeOfType<ConflictError>();
        duplicate.Error.Should().BeOfType<ConflictError>();
        first.Version.Should().Be(2);
        var stored = (await _repository.FindById(taleId, CancellationToken.None))!;
        stored.Status.Should().Be(TaleStatus.Completed);
        stored.Version.Should().Be(2);
    }

    [Test]
    public async Task FindById_CancelledToken_Throws()
    {
        // Arrange
        var taleId = Auid.New("TST");
        await _repository.Save(CreateTestTaleInstance(taleId), CancellationToken.None);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => _repository.FindById(taleId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task StartStory_ViaManager_Persists()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<ITaleRepository>(_repository);
        services.AddSingleton<TestService>();
        services.AddSolTale(assemblies: typeof(InMemoryRepositoryTests).Assembly);
        using var sp = services.BuildServiceProvider();
        var manager = sp.GetRequiredService<TaleManager>();

        // Act
        var start = await manager.StartStory<LifecycleTaleV1, LifecycleInput, LifecycleContext, LifecycleOutput>(
            new LifecycleInput { Value = 1 });

        // Assert
        var persisted = await _repository.FindById(start.Data!.TaleId, CancellationToken.None);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(TaleStatus.WaitingForInput);
        persisted.HandlerTypeName.Should().Be(nameof(LifecycleTaleV1));
    }

    [Test]
    public void Clone_ChapterWithTalePausedError_PreservesErrorType()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        taleInstance.History.Add(new ChapterInfo
        {
            ChapterId = "WaitingChapter",
            StartedAt = DateTime.UtcNow,
            Status = TaleStatus.WaitingForInput,
            Error = new TalePausedError(taleId, "WaitingChapter")
        });

        // Act
        var clone = taleInstance.Clone();

        // Assert
        var error = clone.History[0].Error.Should().BeOfType<TalePausedError>().Subject;
        error.TaleId.Should().Be(taleId);
        error.ChapterId.Should().Be("WaitingChapter");
        error.Recoverable.Should().BeTrue();
    }

    [Test]
    public async Task Repository_SaveAndFind_KeepsLeaseExpiresAtAsTheSameInstant()
    {
        // Arrange
        var taleId = Auid.New("TST");
        var taleInstance = CreateTestTaleInstance(taleId);
        var expiry = new DateTimeOffset(2026, 11, 3, 14, 5, 0, TimeSpan.FromHours(2));
        taleInstance.LeaseExpiresAt = expiry;

        // Act
        await _repository.Save(taleInstance, CancellationToken.None);
        var found = await _repository.FindById(taleId, CancellationToken.None);
        var listed = await _repository.List(cancellationToken: CancellationToken.None);

        // Assert
        found!.LeaseExpiresAt.Should().Be(expiry);
        listed.Should().ContainSingle().Which.LeaseExpiresAt.Should().Be(expiry);
    }

    [Test]
    public async Task Repository_UpdateWithNewLeaseExpiresAt_StoresTheNewInstant()
    {
        // Arrange
        var taleInstance = CreateTestTaleInstance();
        var renewedExpiry = new DateTimeOffset(2026, 11, 3, 12, 10, 0, TimeSpan.Zero);
        taleInstance.LeaseExpiresAt = renewedExpiry.AddMinutes(-5);
        await _repository.Save(taleInstance, CancellationToken.None);
        taleInstance.LeaseExpiresAt = renewedExpiry;

        // Act
        var saved = await _repository.Save(taleInstance, CancellationToken.None);

        // Assert
        saved.IsSuccess.Should().BeTrue();
        var found = (await _repository.FindById(taleInstance.TaleId, CancellationToken.None))!;
        found.LeaseExpiresAt.Should().Be(renewedExpiry);
        found.Version.Should().Be(2);
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
