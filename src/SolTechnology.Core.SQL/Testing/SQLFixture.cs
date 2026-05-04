using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
namespace SolTechnology.Core.SQL.Testing;
/// <summary>
/// Spins up a SQL Server container, deploys a <c>.dacpac</c> built from the supplied
/// <c>.sqlproj</c>, and exposes a ready-to-use <see cref="DatabaseConnectionString"/>.
/// </summary>
/// <remarks>
/// Uses a generic <see cref="ContainerBuilder"/> instead of <c>MsSqlBuilder</c> from
/// <c>Testcontainers.MsSql</c>: the latter forces an
/// <c>UntilUnixCommandIsCompleted</c> wait that shells out to <c>sqlcmd</c> inside
/// the container — a binary that is missing from <c>azure-sql-edge</c>, and whose
/// failure cascades into a confusing <c>Could not find resource 'MsSqlContainer'</c>
/// once the container is auto-removed. Wait is host-side only (log + login probe).
/// </remarks>
public sealed class SQLFixture : IAsyncDisposable
{
    private const string DefaultImage = "mcr.microsoft.com/mssql/server:2022-latest";
    private const string SaPassword = "Strong_p@ssw0rd!";
    private const int InternalPort = 1433;
    private readonly string _databaseName;
    private readonly IContainer _container;
    private string? _sqlProjPath;
    public SQLFixture(string databaseName, string? image = null)
    {
        _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
        _container = new ContainerBuilder()
            .WithImage(image ?? DefaultImage)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
            .WithEnvironment("MSSQL_PID", "Developer")
            .WithPortBinding(InternalPort, assignRandomHostPort: true)
            .WithCleanUp(true)
            // Host-side only: `nc`/`sqlcmd` are not present in some SQL images.
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("SQL Server is now ready for client connections")
                .AddCustomWaitStrategy(new SqlServerLoginWaitStrategy(() => ConnectionString)))
            .Build();
    }
    public string ConnectionString
    {
        get
        {
            var host = _container.Hostname;
            var port = _container.GetMappedPublicPort(InternalPort);
            return new SqlConnectionStringBuilder
            {
                DataSource = $"{host},{port}",
                UserID = "sa",
                Password = SaPassword,
                TrustServerCertificate = true,
                Encrypt = false,
                ConnectTimeout = 30
            }.ToString();
        }
    }
    public string DatabaseConnectionString => new SqlConnectionStringBuilder(ConnectionString)
    {
        InitialCatalog = _databaseName
    }.ToString();
    /// <summary>Path to a .sqlproj that will be built and deployed into the container.</summary>
    public SQLFixture WithSQLProject(string sqlProjPath)
    {
        _sqlProjPath = sqlProjPath;
        return this;
    }
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_sqlProjPath is null)
            throw new InvalidOperationException("Call WithSQLProject(pathToSqlProj) before InitializeAsync.");
        await _container.StartAsync(ct).ConfigureAwait(false);
        await PublishDacpacAsync(_sqlProjPath, ConnectionString, _databaseName, ct).ConfigureAwait(false);
        await using var conn = new SqlConnection(DatabaseConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
    }
    public async ValueTask DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);
    private static async Task PublishDacpacAsync(string sqlProjPath, string serverConnStr, string dbName, CancellationToken ct)
    {
        await Run("dotnet", new[] { "build", sqlProjPath, "-c", "Release" }, ct).ConfigureAwait(false);
        var projDir = Path.GetDirectoryName(sqlProjPath)!;
        var projName = Path.GetFileNameWithoutExtension(sqlProjPath);
        var dacpac = Path.Combine(projDir, "bin", "Release", projName + ".dacpac");
        if (!File.Exists(dacpac))
            throw new FileNotFoundException("Dacpac not found", dacpac);
        var serverCs = new SqlConnectionStringBuilder(serverConnStr) { InitialCatalog = "master" }.ToString();
        var options = new DacDeployOptions
        {
            CreateNewDatabase = true,
            DropObjectsNotInSource = true,
            BlockOnPossibleDataLoss = true
        };
        using var package = DacPackage.Load(dacpac);
        var services = new DacServices(serverCs);
        services.Message += (_, e) => Console.WriteLine(e.Message);
        await Task.Run(() => services.Deploy(package, dbName, upgradeExisting: true, options), ct).ConfigureAwait(false);
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
        var stdout = await p.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        var stderr = await p.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await p.WaitForExitAsync(ct).ConfigureAwait(false);
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"{file} failed ({p.ExitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
    }
}
