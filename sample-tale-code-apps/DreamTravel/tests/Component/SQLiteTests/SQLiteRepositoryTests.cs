using DreamTravel.SQLite;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;

namespace DreamTravel.FunctionalTests.SQLiteTests;

/// <summary>
/// Tests for the SQLite persistence layer.
/// Verifies CRUD operations, thread safety, cross-instance durability, and data integrity.
/// </summary>
[TestFixture]
public class SQLiteRepositoryTests
{
    private string _testDbPath = null!;
    private SQLiteTaleRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_stories_{Guid.NewGuid()}.db");
        _repository = new SQLiteTaleRepository($"Data Source={_testDbPath}");
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
    public async Task SaveAsync_ShouldPersistTaleInstance()
    {
        var storyId = Auid.New("TST");
        var storyInstance = new TaleInstance
        {
            TaleId = storyId,
            HandlerTypeName = "TestHandler",
            Status = TaleStatus.Running,
            Context = "{\"value\":42}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History = [new() { ChapterId = "Chapter1", StartedAt = DateTime.UtcNow, FinishedAt = DateTime.UtcNow }]
        };

        await _repository.Save(storyInstance, CancellationToken.None);

        var retrieved = await _repository.FindById(storyId, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.TaleId.Should().Be(storyId);
        retrieved.HandlerTypeName.Should().Be("TestHandler");
        retrieved.Status.Should().Be(TaleStatus.Running);
        retrieved.Context.Should().Be("{\"value\":42}");
        retrieved.History.Should().HaveCount(1);
        retrieved.History[0].ChapterId.Should().Be("Chapter1");
    }

    [Test]
    public async Task Constructor_DatabaseWithoutVersionColumn_MigratesAndSavesConditionally()
    {
        // Arrange
        var legacyPath = Path.Combine(Path.GetTempPath(), $"test_stories_1x_{Guid.NewGuid()}.db");
        var connectionString = $"Data Source={legacyPath}";
        var taleId = Auid.New("TST");
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE TaleInstances (
                    TaleId TEXT PRIMARY KEY, HandlerTypeName TEXT NOT NULL, IdempotencyKey TEXT,
                    Status INTEGER NOT NULL, Context TEXT NOT NULL, CreatedAt TEXT NOT NULL,
                    LastUpdatedAt TEXT NOT NULL, History TEXT, CurrentChapter TEXT);
                INSERT INTO TaleInstances VALUES
                    (@TaleId, 'TestHandler', NULL, 2, '{}', '2026-11-03T10:00:00.0000000Z', '2026-11-03T10:00:00.0000000Z', NULL, NULL);";
            command.Parameters.AddWithValue("@TaleId", taleId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        try
        {
            // Act
            var repository = new SQLiteTaleRepository(connectionString);
            var legacy = (await repository.FindById(taleId, CancellationToken.None))!;
            var stale = (await repository.FindById(taleId, CancellationToken.None))!;
            legacy.Status = TaleStatus.Completed;
            var saved = await repository.Save(legacy, CancellationToken.None);
            var conflict = await repository.Save(stale, CancellationToken.None);

            // Assert
            saved.IsSuccess.Should().BeTrue();
            legacy.Version.Should().Be(1);
            conflict.Error.Should().BeOfType<ConflictError>();
            var stored = (await repository.FindById(taleId, CancellationToken.None))!;
            stored.Status.Should().Be(TaleStatus.Completed);
            stored.Version.Should().Be(1);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(legacyPath);
        }
    }

    [Test]
    public async Task Save_LeaseExpiresAt_RoundTripsTheInstantInUtc()
    {
        // Arrange
        var running = NewTale(TaleStatus.Running);
        var expiry = new DateTimeOffset(2026, 11, 3, 14, 5, 0, TimeSpan.FromHours(2));
        running.LeaseExpiresAt = expiry;
        var paused = NewTale(TaleStatus.WaitingForInput);

        // Act
        await _repository.Save(running, CancellationToken.None);
        await _repository.Save(paused, CancellationToken.None);

        // Assert
        var storedRunning = (await _repository.FindById(running.TaleId, CancellationToken.None))!;
        storedRunning.LeaseExpiresAt.Should().Be(expiry);
        storedRunning.LeaseExpiresAt!.Value.Offset.Should().Be(TimeSpan.Zero);
        (await _repository.FindById(paused.TaleId, CancellationToken.None))!.LeaseExpiresAt.Should().BeNull();
    }

    [Test]
    public async Task Save_UpdateWithNewLeaseExpiresAt_StoresTheNewInstant()
    {
        // Arrange
        var running = NewTale(TaleStatus.Running);
        var renewedExpiry = new DateTimeOffset(2026, 11, 3, 12, 10, 0, TimeSpan.Zero);
        running.LeaseExpiresAt = renewedExpiry.AddMinutes(-5);
        await _repository.Save(running, CancellationToken.None);
        running.LeaseExpiresAt = renewedExpiry;

        // Act
        var saved = await _repository.Save(running, CancellationToken.None);

        // Assert
        saved.IsSuccess.Should().BeTrue();
        var stored = (await _repository.FindById(running.TaleId, CancellationToken.None))!;
        stored.LeaseExpiresAt.Should().Be(renewedExpiry);
        stored.Version.Should().Be(2);
    }

    [Test]
    public async Task Constructor_DatabaseWithoutLeaseExpiresAtColumn_MigratesAndStoresTheLease()
    {
        // Arrange
        var legacyPath = Path.Combine(Path.GetTempPath(), $"test_stories_lease_{Guid.NewGuid()}.db");
        var connectionString = $"Data Source={legacyPath}";
        var taleId = Auid.New("TST");
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE TaleInstances (
                    TaleId TEXT PRIMARY KEY, HandlerTypeName TEXT NOT NULL, IdempotencyKey TEXT,
                    Status INTEGER NOT NULL, Context TEXT NOT NULL, CreatedAt TEXT NOT NULL,
                    LastUpdatedAt TEXT NOT NULL, History TEXT, CurrentChapter TEXT,
                    Version INTEGER NOT NULL DEFAULT 0);
                INSERT INTO TaleInstances VALUES
                    (@TaleId, 'TestHandler', NULL, 1, '{}', '2026-11-03T10:00:00.0000000Z', '2026-11-03T10:00:00.0000000Z', NULL, NULL, 3);";
            command.Parameters.AddWithValue("@TaleId", taleId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        try
        {
            // Act
            var repository = new SQLiteTaleRepository(connectionString);
            var legacy = (await repository.FindById(taleId, CancellationToken.None))!;
            var leaseExpiresAtBeforeSave = legacy.LeaseExpiresAt;
            var expiry = new DateTimeOffset(2026, 11, 3, 10, 5, 0, TimeSpan.Zero);
            legacy.LeaseExpiresAt = expiry;
            var saved = await repository.Save(legacy, CancellationToken.None);

            // Assert
            leaseExpiresAtBeforeSave.Should().BeNull();
            saved.IsSuccess.Should().BeTrue();
            var stored = (await repository.FindById(taleId, CancellationToken.None))!;
            stored.LeaseExpiresAt.Should().Be(expiry);
            stored.Version.Should().Be(4);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(legacyPath);
        }
    }

    [Test]
    public async Task FindById_ShouldReturnNull_WhenTaleDoesNotExist()
    {
        var result = await _repository.FindById(Auid.New("TST"), CancellationToken.None);
        result.Should().BeNull();
    }

    [Test]
    public async Task SaveAsync_ShouldUpdateExistingTale()
    {
        var storyId = Auid.New("TST");
        var storyInstance = new TaleInstance
        {
            TaleId = storyId,
            HandlerTypeName = "TestHandler",
            Status = TaleStatus.Running,
            Context = "{\"value\":1}",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.Save(storyInstance, CancellationToken.None);

        storyInstance.Status = TaleStatus.Completed;
        storyInstance.Context = "{\"value\":2}";
        storyInstance.LastUpdatedAt = DateTime.UtcNow.AddMinutes(5);
        await _repository.Save(storyInstance, CancellationToken.None);

        var retrieved = await _repository.FindById(storyId, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(TaleStatus.Completed);
        retrieved.Context.Should().Be("{\"value\":2}");
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveTale()
    {
        var storyId = Auid.New("TST");
        var storyInstance = new TaleInstance
        {
            TaleId = storyId, HandlerTypeName = "TestHandler", Status = TaleStatus.Running,
            Context = "{}", CreatedAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.Save(storyInstance, CancellationToken.None);
        await _repository.Delete(storyId, CancellationToken.None);

        var result = await _repository.FindById(storyId, CancellationToken.None);
        result.Should().BeNull();
    }

    [Test]
    public async Task SaveAsync_ShouldHandleCurrentChapter()
    {
        var storyId = Auid.New("TST");
        var currentChapter = new ChapterInfo { ChapterId = "CurrentChapter", StartedAt = DateTime.UtcNow };
        var storyInstance = new TaleInstance
        {
            TaleId = storyId, HandlerTypeName = "TestHandler", Status = TaleStatus.WaitingForInput,
            Context = "{}", CreatedAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow,
            CurrentChapter = currentChapter
        };

        await _repository.Save(storyInstance, CancellationToken.None);

        var retrieved = await _repository.FindById(storyId, CancellationToken.None);
        retrieved!.CurrentChapter.Should().NotBeNull();
        retrieved.CurrentChapter!.ChapterId.Should().Be("CurrentChapter");
    }

    [Test]
    public async Task SaveAsync_ShouldBeThreadSafe()
    {
        var storyIds = Enumerable.Range(0, 50).Select(_ => Auid.New("TST")).ToList();

        var tasks = storyIds.Select((id, i) => Task.Run(async () =>
        {
            var instance = new TaleInstance
            {
                TaleId = id, HandlerTypeName = "ConcurrentHandler", Status = TaleStatus.Running,
                Context = $"{{\"index\":{i}}}", CreatedAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow
            };
            await _repository.Save(instance, CancellationToken.None);
        }));

        await Task.WhenAll(tasks);

        foreach (var (id, i) in storyIds.Select((id, i) => (id, i)))
        {
            var retrieved = await _repository.FindById(id, CancellationToken.None);
            retrieved.Should().NotBeNull();
            retrieved!.Context.Should().Contain($"\"index\":{i}");
        }
    }

    [Test]
    public async Task Repository_ShouldPersistAcrossInstances()
    {
        var storyId = Auid.New("TST");
        var storyInstance = new TaleInstance
        {
            TaleId = storyId, HandlerTypeName = "TestHandler", Status = TaleStatus.Running,
            Context = "{\"persisted\":true}", CreatedAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow
        };

        await _repository.Save(storyInstance, CancellationToken.None);

        var newRepository = new SQLiteTaleRepository($"Data Source={_testDbPath}");
        var retrieved = await newRepository.FindById(storyId, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.Context.Should().Be("{\"persisted\":true}");
    }

    private static TaleInstance NewTale(TaleStatus status) => new()
    {
        TaleId = Auid.New("TST"), HandlerTypeName = "TestHandler", Status = status,
        Context = "{}", CreatedAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow
    };
}

