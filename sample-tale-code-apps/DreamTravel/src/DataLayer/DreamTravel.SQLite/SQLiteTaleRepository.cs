using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace DreamTravel.SQLite;

/// <summary>
/// SQLite implementation of <see cref="ITaleRepository"/>. Configured via
/// <see cref="SQLiteTaleRepositoryOptions"/> (connection string + tuning). Enables WAL
/// journal mode for concurrency and retries on <c>SQLITE_BUSY</c>/<c>SQLITE_LOCKED</c>.
/// <para>
/// <b>Static state:</b> database initialization (schema creation, WAL pragma) is cached per
/// connection string in a static <c>HashSet</c>. This avoids redundant DDL on every request but
/// means the cache survives across test runs in the same process. Use unique temp-file connection
/// strings in tests to avoid cross-contamination.
/// </para>
/// </summary>
public class SQLiteTaleRepository : ITaleRepository
{
    private readonly SQLiteTaleRepositoryOptions _options;
    private readonly string _connectionString;

    private static readonly object _initLock = new();
    private static readonly HashSet<string> _initializedConnections =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a repository using the provided options. When <paramref name="options"/> is
    /// <c>null</c>, the defaults from <see cref="SQLiteTaleRepositoryOptions"/> are used
    /// (database under LocalApplicationData).
    /// </summary>
    public SQLiteTaleRepository(SQLiteTaleRepositoryOptions? options = null)
    {
        _options = options ?? new SQLiteTaleRepositoryOptions();
        _connectionString = ValidateConnectionString(_options.ConnectionString);
        EnsureDatabaseInitialized();
    }

    /// <summary>Convenience ctor for a connection string without options.</summary>
    public SQLiteTaleRepository(string connectionString)
        : this(new SQLiteTaleRepositoryOptions { ConnectionString = connectionString }) { }

    private static string ValidateConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

        try
        {
            _ = new SqliteConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"Invalid SQLite connection string: {ex.Message}", nameof(connectionString), ex);
        }

        return connectionString;
    }

    private void EnsureDatabaseInitialized()
    {
        lock (_initLock)
        {
            if (_initializedConnections.Contains(_connectionString)) return;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            if (_options.EnableWalMode || _options.SynchronousNormal)
            {
                using var pragma = connection.CreateCommand();
                var parts = new List<string>();
                if (_options.EnableWalMode) parts.Add("PRAGMA journal_mode=WAL;");
                if (_options.SynchronousNormal) parts.Add("PRAGMA synchronous=NORMAL;");
                pragma.CommandText = string.Concat(parts);
                pragma.ExecuteNonQuery();
            }

            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS TaleInstances (
                    TaleId TEXT PRIMARY KEY,
                    HandlerTypeName TEXT NOT NULL,
                    IdempotencyKey TEXT,
                    Status INTEGER NOT NULL,
                    Context TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    LastUpdatedAt TEXT NOT NULL,
                    History TEXT,
                    CurrentChapter TEXT
                );

                CREATE INDEX IF NOT EXISTS IX_TaleInstances_Status ON TaleInstances(Status);
                CREATE INDEX IF NOT EXISTS IX_TaleInstances_LastUpdatedAt ON TaleInstances(LastUpdatedAt);
                CREATE INDEX IF NOT EXISTS IX_TaleInstances_HandlerTypeName ON TaleInstances(HandlerTypeName);
                CREATE UNIQUE INDEX IF NOT EXISTS UX_TaleInstances_IdempotencyKey
                    ON TaleInstances(IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
            ";
            createTableCommand.ExecuteNonQuery();

            MigrateSchema(connection);

            _initializedConnections.Add(_connectionString);
        }
    }

    /// <summary>Add columns missing in older schemas.</summary>
    private static void MigrateSchema(SqliteConnection connection)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(TaleInstances);";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                existing.Add(reader.GetString(1));
            }
        }

        void AddColumn(string name, string type)
        {
            if (existing.Contains(name)) return;
            using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE TaleInstances ADD COLUMN {name} {type};";
            alter.ExecuteNonQuery();
        }

        AddColumn("IdempotencyKey", "TEXT");
    }

    public Task<TaleInstance?> FindById(Auid storyId) =>
        ExecuteWithRetry(async () =>
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = SelectSql + " WHERE TaleId = @TaleId";
            command.Parameters.AddWithValue("@TaleId", storyId.ToString());

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapRow(reader) : null;
        });

    public Task<TaleInstance?> FindByIdempotencyKey(string idempotencyKey) =>
        ExecuteWithRetry(async () =>
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = SelectSql + " WHERE IdempotencyKey = @Key LIMIT 1";
            command.Parameters.AddWithValue("@Key", idempotencyKey);

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapRow(reader) : null;
        });

    public async Task<IReadOnlyList<TaleInstance>> ListAsync(
        TaleStatus? status = null,
        string? handlerTypeName = null,
        int skip = 0,
        int take = 100)
    {
        return await ExecuteWithRetry(async () =>
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            var sql = SelectSql + " WHERE 1=1";
            if (status.HasValue)
            {
                sql += " AND Status = @Status";
                command.Parameters.AddWithValue("@Status", (int)status.Value);
            }
            if (!string.IsNullOrEmpty(handlerTypeName))
            {
                sql += " AND HandlerTypeName = @Handler";
                command.Parameters.AddWithValue("@Handler", handlerTypeName);
            }
            sql += " ORDER BY LastUpdatedAt DESC LIMIT @Take OFFSET @Skip";
            command.Parameters.AddWithValue("@Take", take);
            command.Parameters.AddWithValue("@Skip", skip);
            command.CommandText = sql;

            var results = new List<TaleInstance>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) results.Add(MapRow(reader));
            return (IReadOnlyList<TaleInstance>)results;
        });
    }

    public Task SaveAsync(TaleInstance storyInstance) =>
        ExecuteWithRetry(async () =>
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO TaleInstances
                    (TaleId, HandlerTypeName, IdempotencyKey, Status, Context,
                     CreatedAt, LastUpdatedAt, History, CurrentChapter)
                VALUES
                    (@TaleId, @HandlerTypeName, @IdempotencyKey, @Status, @Context,
                     @CreatedAt, @LastUpdatedAt, @History, @CurrentChapter)
                ON CONFLICT(TaleId) DO UPDATE SET
                    IdempotencyKey = COALESCE(@IdempotencyKey, TaleInstances.IdempotencyKey),
                    Status = @Status,
                    Context = @Context,
                    LastUpdatedAt = @LastUpdatedAt,
                    History = @History,
                    CurrentChapter = @CurrentChapter
            ";

            command.Parameters.AddWithValue("@TaleId", storyInstance.TaleId.ToString());
            command.Parameters.AddWithValue("@HandlerTypeName", storyInstance.HandlerTypeName);
            command.Parameters.AddWithValue("@IdempotencyKey", (object?)storyInstance.IdempotencyKey ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (int)storyInstance.Status);
            command.Parameters.AddWithValue("@Context", storyInstance.Context);
            command.Parameters.AddWithValue("@CreatedAt", storyInstance.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("@LastUpdatedAt", storyInstance.LastUpdatedAt.ToString("O"));
            command.Parameters.AddWithValue("@History",
                storyInstance.History is { Count: > 0 }
                    ? (object)JsonSerializer.Serialize(storyInstance.History)
                    : DBNull.Value);
            command.Parameters.AddWithValue("@CurrentChapter",
                storyInstance.CurrentChapter != null
                    ? (object)JsonSerializer.Serialize(storyInstance.CurrentChapter)
                    : DBNull.Value);

            await command.ExecuteNonQueryAsync();
            return 0;
        });

    public Task DeleteAsync(Auid storyId) =>
        ExecuteWithRetry(async () =>
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM TaleInstances WHERE TaleId = @TaleId";
            command.Parameters.AddWithValue("@TaleId", storyId.ToString());

            await command.ExecuteNonQueryAsync();
            return 0;
        });

    private const string SelectSql = @"
        SELECT TaleId, HandlerTypeName, IdempotencyKey, Status, Context,
               CreatedAt, LastUpdatedAt, History, CurrentChapter
        FROM TaleInstances";

    private static TaleInstance MapRow(SqliteDataReader reader) => new()
    {
        TaleId = Auid.Parse(reader.GetString(0)),
        HandlerTypeName = reader.GetString(1),
        IdempotencyKey = reader.IsDBNull(2) ? null : reader.GetString(2),
        Status = (TaleStatus)reader.GetInt32(3),
        Context = reader.GetString(4),
        CreatedAt = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        LastUpdatedAt = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        History = reader.IsDBNull(7)
            ? new List<ChapterInfo>()
            : JsonSerializer.Deserialize<List<ChapterInfo>>(reader.GetString(7)) ?? new List<ChapterInfo>(),
        CurrentChapter = reader.IsDBNull(8)
            ? null
            : JsonSerializer.Deserialize<ChapterInfo>(reader.GetString(8))
    };

    private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (SqliteException ex) when (
                (ex.SqliteErrorCode == 5 /* SQLITE_BUSY */ ||
                 ex.SqliteErrorCode == 6 /* SQLITE_LOCKED */) &&
                attempt < _options.MaxRetries)
            {
                attempt++;
                await Task.Delay(_options.RetryBaseDelay * attempt);
            }
        }
    }
}

