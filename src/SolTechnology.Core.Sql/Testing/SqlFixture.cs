using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace SolTechnology.Core.Sql.Testing;

public class SqlFixture : IAsyncDisposable
{
    private const string MssqlImage = "mcr.microsoft.com/mssql/server:2022-latest";
    private static readonly INetwork Network = new NetworkBuilder().Build();
    private const string NetworkAlias = "testing-network";
    
    private readonly MsSqlContainer _container;
    private readonly string? _databaseName;
    private readonly List<string> _sqlScriptPaths = new();
    private readonly List<string> _sqlScripts = new();
    private readonly List<(string Path, bool Recursive, string? Pattern)> _sqlFolders = new();

    public SqlFixture(
        string databaseName)
    {
        _databaseName = databaseName;
        
        _container = new MsSqlBuilder()
            .WithImage(MssqlImage)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .WithNetwork(Network)
            .WithNetworkAliases(NetworkAlias)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(1433)
                .UntilMessageIsLogged("SQL Server is now ready for client connections"))
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public string DatabaseConnectionString => 
        new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = _databaseName
        }.ToString();

    /// <summary>
    /// Add all SQL files from specified folder
    /// </summary>
    /// <param name="folderPath">Path to folder containing SQL files</param>
    /// <param name="recursive">Search in subdirectories</param>
    /// <param name="searchPattern">File pattern (default: "*.sql")</param>
    public SqlFixture WithSqlFolder(
        string folderPath, 
        bool recursive = true, 
        string searchPattern = "*.sql")
    {
        _sqlFolders.Add((folderPath, recursive, searchPattern));
        return this;
    }

    /// <summary>
    /// Add SQL script files to be executed after container starts
    /// </summary>
    public SqlFixture WithSqlScriptFiles(params string[] scriptPaths)
    {
        _sqlScriptPaths.AddRange(scriptPaths);
        return this;
    }

    /// <summary>
    /// Add inline SQL scripts to be executed after container starts
    /// </summary>
    public SqlFixture WithSqlScripts(params string[] scripts)
    {
        _sqlScripts.AddRange(scripts);
        return this;
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await InitializeDatabaseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        // Create database
        await ExecuteNonQueryAsync(GetCreateDatabaseScript(), "master");

        // Execute SQL files from folders
        var folderFiles = GetSqlFilesFromFolders();
        foreach (var filePath in folderFiles)
        {
            Console.WriteLine($"Executing SQL script: {filePath}");
            var script = await File.ReadAllTextAsync(filePath);
            await ExecuteScriptAsync(script, filePath);
        }

        // Execute individual SQL script files
        foreach (var scriptPath in _sqlScriptPaths)
        {
            if (File.Exists(scriptPath))
            {
                Console.WriteLine($"Executing SQL script: {Path.GetFileName(scriptPath)}");
                var script = await File.ReadAllTextAsync(scriptPath);
                await ExecuteScriptAsync(script, Path.GetFileName(scriptPath));
            }
            else
            {
                throw new FileNotFoundException($"SQL script file not found: {scriptPath}");
            }
        }

        // Execute inline scripts
        var scriptIndex = 0;
        foreach (var script in _sqlScripts)
        {
            Console.WriteLine($"Executing inline SQL script #{++scriptIndex}");
            await ExecuteScriptAsync(script, $"Inline script #{scriptIndex}");
        }
    }
 private List<string> GetSqlFilesFromFolders()
    {
        var files = new List<string>();
        
        foreach (var (folderPath, recursive, pattern) in _sqlFolders)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"SQL scripts folder not found: {folderPath}");
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var sqlFiles = Directory.GetFiles(folderPath, pattern ?? "*.sql", searchOption);

            foreach (var file in sqlFiles)
            {
                files.Add(file);
            }
        }

        // Sortuj pliki według zależności
        return SqlScriptOrderer.OrderByDependencies(files).ToList();
    }

    private async Task ExecuteScriptAsync(string script, string? scriptName = null)
    {
        try
        {
            // Split by GO statements for batch execution
            var batches = script.Split(
                new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO", "\nGO", "GO\r\n", "GO\n" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var batch in batches.Where(b => !string.IsNullOrWhiteSpace(b)))
            {
                await ExecuteNonQueryAsync(batch, _databaseName);
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"Failed to execute SQL script{(scriptName != null ? $" '{scriptName}'" : "")}: {ex.Message}", 
                ex);
        }
    }

    private async Task ExecuteNonQueryAsync(string commandText, string? database = null)
    {
        var connectionString = database != null
            ? new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = database
            }.ToString()
            : DatabaseConnectionString;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(commandText, connection)
        {
            CommandTimeout = 60 // Increase timeout for complex scripts
        };
        await command.ExecuteNonQueryAsync();
    }

    private string GetCreateDatabaseScript() => $"""
        IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{_databaseName}')
        BEGIN
            CREATE DATABASE [{_databaseName}]
        END
        """;
}