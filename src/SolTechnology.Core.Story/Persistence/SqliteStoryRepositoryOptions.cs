namespace SolTechnology.Core.Story.Persistence;

/// <summary>
/// Configuration for <see cref="SqliteStoryRepository"/>. Exposed so the registration
/// helper <c>UseSqliteStoryRepository</c> can be called either with a connection string
/// shortcut or with a configure callback for fine-grained tuning.
/// </summary>
public sealed class SqliteStoryRepositoryOptions
{
    /// <summary>
    /// ADO.NET connection string. Defaults to a file under
    /// <c>%LOCALAPPDATA%/SolTechnology/StoryFramework/stories.db</c>.
    /// Recommended explicit value: <c>"Data Source=stories.db"</c>.
    /// </summary>
    public string ConnectionString { get; set; } = BuildDefaultConnectionString();

    /// <summary>
    /// Maximum retries when a command fails with <c>SQLITE_BUSY</c> or <c>SQLITE_LOCKED</c>.
    /// Set to 0 to disable retries (the operation throws immediately).
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Base delay applied between retries; actual delay is <c>BaseDelay × attemptNumber</c>.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(25);

    /// <summary>Enables <c>PRAGMA journal_mode=WAL</c> on first use of a given connection string.</summary>
    public bool EnableWalMode { get; set; } = true;

    /// <summary>Enables <c>PRAGMA synchronous=NORMAL</c> — recommended alongside WAL.</summary>
    public bool SynchronousNormal { get; set; } = true;

    private static string BuildDefaultConnectionString()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appData, "SolTechnology", "StoryFramework");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "stories.db");
        return $"Data Source={path};Mode=ReadWriteCreate;Cache=Shared";
    }
}

