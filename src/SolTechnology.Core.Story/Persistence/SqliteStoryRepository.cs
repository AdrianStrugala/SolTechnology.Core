using System.Text.Json;
using Microsoft.Data.Sqlite;
using SolTechnology.Core.Story.Models;

namespace SolTechnology.Core.Story.Persistence;

/// <summary>
/// SQLite implementation of IStoryRepository.
/// Story instances are persisted to a SQLite database file.
/// Data survives application restarts.
/// Thread-safe with connection pooling.
/// </summary>
public class SqliteStoryRepository : IStoryRepository
{
    private readonly string _connectionString;
    private readonly object _initLock = new();
    private bool _isInitialized;

    public SqliteStoryRepository(string? dbPath = null)
    {
        var path = dbPath ?? GetDefaultDbPath();
        _connectionString = $"Data Source={path};Mode=ReadWriteCreate;Cache=Shared";
        EnsureDatabaseInitialized();
    }

    private static string GetDefaultDbPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appData, "SolTechnology", "StoryFramework");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "stories.db");
    }

    private void EnsureDatabaseInitialized()
    {
        if (_isInitialized) return;

        lock (_initLock)
        {
            if (_isInitialized) return;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS StoryInstances (
                    StoryId TEXT PRIMARY KEY,
                    HandlerTypeName TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    Context TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    LastUpdatedAt TEXT NOT NULL,
                    History TEXT,
                    CurrentChapter TEXT
                );

                CREATE INDEX IF NOT EXISTS IX_StoryInstances_Status
                    ON StoryInstances(Status);

                CREATE INDEX IF NOT EXISTS IX_StoryInstances_LastUpdatedAt
                    ON StoryInstances(LastUpdatedAt);
            ";
            createTableCommand.ExecuteNonQuery();

            _isInitialized = true;
        }
    }

    public async Task<StoryInstance?> FindById(string storyId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT StoryId, HandlerTypeName, Status, Context, CreatedAt, LastUpdatedAt, History, CurrentChapter
            FROM StoryInstances
            WHERE StoryId = @StoryId
        ";
        command.Parameters.AddWithValue("@StoryId", storyId);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var storyInstance = new StoryInstance
        {
            StoryId = reader.GetString(0),
            HandlerTypeName = reader.GetString(1),
            Status = (StoryStatus)reader.GetInt32(2),
            Context = reader.GetString(3),
            CreatedAt = DateTime.Parse(reader.GetString(4)),
            LastUpdatedAt = DateTime.Parse(reader.GetString(5)),
            History = reader.IsDBNull(6)
                ? new List<ChapterInfo>()
                : JsonSerializer.Deserialize<List<ChapterInfo>>(reader.GetString(6)) ?? new List<ChapterInfo>(),
            CurrentChapter = reader.IsDBNull(7)
                ? null
                : JsonSerializer.Deserialize<ChapterInfo>(reader.GetString(7))
        };

        return storyInstance;
    }

    public async Task SaveAsync(StoryInstance storyInstance)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO StoryInstances (StoryId, HandlerTypeName, Status, Context, CreatedAt, LastUpdatedAt, History, CurrentChapter)
            VALUES (@StoryId, @HandlerTypeName, @Status, @Context, @CreatedAt, @LastUpdatedAt, @History, @CurrentChapter)
            ON CONFLICT(StoryId) DO UPDATE SET
                Status = @Status,
                Context = @Context,
                LastUpdatedAt = @LastUpdatedAt,
                History = @History,
                CurrentChapter = @CurrentChapter
        ";

        command.Parameters.AddWithValue("@StoryId", storyInstance.StoryId);
        command.Parameters.AddWithValue("@HandlerTypeName", storyInstance.HandlerTypeName);
        command.Parameters.AddWithValue("@Status", (int)storyInstance.Status);
        command.Parameters.AddWithValue("@Context", storyInstance.Context);
        command.Parameters.AddWithValue("@CreatedAt", storyInstance.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@LastUpdatedAt", storyInstance.LastUpdatedAt.ToString("O"));
        command.Parameters.AddWithValue("@History",
            storyInstance.History != null && storyInstance.History.Any()
                ? JsonSerializer.Serialize(storyInstance.History)
                : DBNull.Value);
        command.Parameters.AddWithValue("@CurrentChapter",
            storyInstance.CurrentChapter != null
                ? JsonSerializer.Serialize(storyInstance.CurrentChapter)
                : DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string storyId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM StoryInstances WHERE StoryId = @StoryId";
        command.Parameters.AddWithValue("@StoryId", storyId);

        await command.ExecuteNonQueryAsync();
    }
}
