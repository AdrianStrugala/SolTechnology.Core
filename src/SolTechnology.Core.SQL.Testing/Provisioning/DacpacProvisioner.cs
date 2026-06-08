using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;

namespace SolTechnology.Core.SQL.Testing.Provisioning;

/// <summary>
/// Builds a <c>.dacpac</c> from a <c>.sqlproj</c> and deploys it into the (MSSQL) container,
/// creating the target database. MSSQL only.
/// </summary>
internal sealed class DacpacProvisioner(string sqlProjPath) : ISchemaProvisioner
{
    public bool CreatesDatabase => true;

    public async Task ProvisionAsync(SQLFixture fixture, CancellationToken ct)
    {
        if (fixture.Provider != SQLProvider.SQLServer)
        {
            throw new InvalidOperationException(
                "Dacpac provisioning (WithSQLProject) is only supported for SQL Server. Use WithScripts or WithSchema for Postgres.");
        }

        await Run("dotnet", ["build", sqlProjPath, "-c", "Release"], ct).ConfigureAwait(false);

        var projDir = Path.GetDirectoryName(sqlProjPath)!;
        var projName = Path.GetFileNameWithoutExtension(sqlProjPath);
        var dacpac = Path.Combine(projDir, "bin", "Release", projName + ".dacpac");
        if (!File.Exists(dacpac))
        {
            throw new FileNotFoundException("Dacpac not found", dacpac);
        }

        var serverCs = new SqlConnectionStringBuilder(fixture.ConnectionString) { InitialCatalog = "master" }.ToString();
        var options = new DacDeployOptions
        {
            CreateNewDatabase = true,
            DropObjectsNotInSource = true,
            BlockOnPossibleDataLoss = true
        };

        using var package = DacPackage.Load(dacpac);
        var services = new DacServices(serverCs);
        services.Message += (_, e) => Console.WriteLine(e.Message);
        await Task.Run(() => services.Deploy(package, fixture.DatabaseName, upgradeExisting: true, options), ct)
            .ConfigureAwait(false);
    }

    private static async Task Run(string file, IEnumerable<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        using var p = new Process { StartInfo = psi };
        p.Start();
        var stdout = await p.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        var stderr = await p.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await p.WaitForExitAsync(ct).ConfigureAwait(false);
        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException($"{file} failed ({p.ExitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
    }
}



