using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Testcontainers.MsSql;

namespace SolTechnology.Core.Sql.Testing;

public sealed class SqlFixture(string databaseName) : IAsyncDisposable
{
    private const string MssqlImage = "mcr.microsoft.com/azure-sql-edge:latest";
    private static readonly INetwork Network = new NetworkBuilder().Build();
    private const string NetworkAlias = "testing-network";
    
    private readonly MsSqlContainer _container = new MsSqlBuilder()
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

    private string? _sqlProjPath;

    public string ConnectionString => _container.GetConnectionString();
    public string DatabaseConnectionString => new SqlConnectionStringBuilder(ConnectionString)
    {
        InitialCatalog = databaseName
    }.ToString();

    // Ścieżka do .sqlproj, np. ../../db/DreamTravelDatabase/DreamTravelDatabase.sqlproj
    public SqlFixture WithSqlProject(string sqlProjPath)
    {
        _sqlProjPath = sqlProjPath;
        return this;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_sqlProjPath is null)
            throw new InvalidOperationException("Call WithSqlProject(pathToSqlProj) before InitializeAsync.");

        await _container.StartAsync(ct);

        // Publikuj .sqlproj -> kontener
        await PublishDacpacAsync(_sqlProjPath, ConnectionString, databaseName, ct);

        // sanity check, że DB istnieje
        await using var conn = new SqlConnection(DatabaseConnectionString);
        await conn.OpenAsync(ct);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    private static async Task PublishDacpacAsync(string sqlProjPath, string serverConnStr, string dbName, CancellationToken ct)
    {
        // 1) Build dacpac
        await Run("dotnet", new[] { "build", sqlProjPath, "-c", "Release" }, ct);

        // 2) Wyznacz ścieżkę do dacpac
        var projDir  = Path.GetDirectoryName(sqlProjPath)!;
        var projName = Path.GetFileNameWithoutExtension(sqlProjPath);
        var dacpac   = Path.Combine(projDir, "bin", "Release", projName + ".dacpac");

        if (!File.Exists(dacpac))
            throw new FileNotFoundException("Dacpac not found", dacpac);

        // 3) Deploy dacpac
        var serverCs = new SqlConnectionStringBuilder(serverConnStr) { InitialCatalog = "master" }.ToString();
        var options = new DacDeployOptions
        {
            CreateNewDatabase = true,
            DropObjectsNotInSource = true,
            BlockOnPossibleDataLoss = true,
            // W razie potrzeby:
            // AllowIncompatiblePlatform = true (DacFx nie ma tego jako opcji, ale i tak zadziała)
        };

        using var package = DacPackage.Load(dacpac);
        var services = new DacServices(serverCs);
        services.Message += (_, e) => Console.WriteLine(e.Message);

        await Task.Run(() => services.Deploy(package, dbName, upgradeExisting: true, options), ct);
    }

    private static async Task Run(string file, IEnumerable<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = new Process { StartInfo = psi };
        p.Start();
        var stdout = await p.StandardOutput.ReadToEndAsync(ct);
        var stderr = await p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0) throw new InvalidOperationException($"{file} failed ({p.ExitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
    }
}