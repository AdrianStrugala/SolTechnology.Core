using SolTechnology.Core.Story.Persistence;

namespace SolTechnology.Core.Story;

/// <summary>
/// Configuration options for story execution.
/// Use this to opt-in to advanced features like persistence and REST API.
/// By default, stories execute immediately without persistence (simple mode).
/// </summary>
public class StoryOptions
{
    /// <summary>
    /// Default options for simple, immediate story execution without persistence.
    /// Use this when you don't need pause/resume functionality.
    /// </summary>
    public static StoryOptions Default => new();

    /// <summary>
    /// Enable workflow state persistence for pausable stories.
    /// Required for stories with interactive chapters that need to pause and resume.
    /// </summary>
    public bool EnablePersistence { get; init; } = false;

    /// <summary>
    /// Repository for workflow state persistence.
    /// Required if EnablePersistence is true.
    /// Use WithInMemoryPersistence() or WithSqlitePersistence() factory methods.
    /// </summary>
    public IStoryRepository? Repository { get; init; }

    /// <summary>
    /// Enable REST API endpoints for story control via StoryController.
    /// Requires EnablePersistence to be true.
    /// </summary>
    public bool EnableRestApi { get; init; } = false;

    /// <summary>
    /// Stop story execution on the first chapter error (true) or collect all errors (false).
    /// When false, all chapters execute and errors are aggregated into AggregateError.
    /// When true, execution stops at the first failure.
    /// Default is true for intuitive fail-fast behavior.
    /// </summary>
    public bool StopOnFirstError { get; init; } = true;

    /// <summary>
    /// Factory method for persistence-enabled stories with in-memory repository.
    /// Story state is kept in memory - will be lost on application restart.
    /// Good for development and testing.
    /// </summary>
    /// <returns>StoryOptions configured with in-memory persistence</returns>
    public static StoryOptions WithInMemoryPersistence() => new()
    {
        EnablePersistence = true,
        Repository = new InMemoryStoryRepository()
    };

    /// <summary>
    /// Factory method for persistence-enabled stories with SQLite repository.
    /// Story state is persisted to SQLite database file.
    /// Survives application restarts.
    /// </summary>
    /// <param name="dbPath">
    /// Optional path to SQLite database file.
    /// If null, uses default location: %LOCALAPPDATA%/SolTechnology/StoryFramework/stories.db
    /// </param>
    /// <returns>StoryOptions configured with SQLite persistence</returns>
    public static StoryOptions WithSqlitePersistence(string? dbPath = null) => new()
    {
        EnablePersistence = true,
        Repository = new SqliteStoryRepository(dbPath)
    };
}
