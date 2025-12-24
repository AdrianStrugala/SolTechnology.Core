using SolTechnology.Core.Story.Models;

namespace SolTechnology.Core.Story.Persistence;

/// <summary>
/// SQLite implementation of IStoryRepository.
/// Story instances are persisted to a SQLite database file.
/// Data survives application restarts.
/// TODO: Full implementation coming in Week 3.
/// </summary>
public class SqliteStoryRepository : IStoryRepository
{
    private readonly string _dbPath;

    public SqliteStoryRepository(string? dbPath = null)
    {
        _dbPath = dbPath ?? GetDefaultDbPath();
        // TODO: Initialize database schema
    }

    private static string GetDefaultDbPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appData, "SolTechnology", "StoryFramework");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "stories.db");
    }

    public Task<StoryInstance?> FindById(string storyId)
    {
        // TODO: Implement SQLite query
        throw new NotImplementedException("SQLite persistence will be implemented in Week 3");
    }

    public Task SaveAsync(StoryInstance storyInstance)
    {
        // TODO: Implement SQLite upsert
        throw new NotImplementedException("SQLite persistence will be implemented in Week 3");
    }

    public Task DeleteAsync(string storyId)
    {
        // TODO: Implement SQLite delete
        throw new NotImplementedException("SQLite persistence will be implemented in Week 3");
    }
}
