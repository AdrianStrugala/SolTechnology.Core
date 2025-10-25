namespace SolTechnology.Core.Sql.SqlProject;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;

public static class SqlProjectDeployer
{
    public static async Task DeployAsync(
        string sqlProjPath, 
        string connectionString, 
        string databaseName,
        CancellationToken ct = default)
    {
        // 1) Build sql proj
        await RunProcess("dotnet", ["build", sqlProjPath, "-c", "Release"], ct);

        // 2) Find dacpac
        var projDir = Path.GetDirectoryName(sqlProjPath)!;
        var projName = Path.GetFileNameWithoutExtension(sqlProjPath);
        var dacpacPath = Path.Combine(projDir, "bin", "Release", projName + ".dacpac");

        if (!File.Exists(dacpacPath))
            throw new FileNotFoundException($"Dacpac not found: {dacpacPath}");

        Console.WriteLine($"Deploying dacpac: {dacpacPath}");

        // 3) Deploy dacpac
        var masterConnStr = new SqlConnectionStringBuilder(connectionString) 
        { 
            InitialCatalog = "master" 
        }.ToString();

        var options = new DacDeployOptions
        {
            CreateNewDatabase = true,
            DropObjectsNotInSource = false,
            BlockOnPossibleDataLoss = false
        };

        using var package = DacPackage.Load(dacpacPath);
        var services = new DacServices(masterConnStr);
        
        services.Message += (_, e) => Console.WriteLine($"[DacPac] {e.Message}");
        services.ProgressChanged += (_, e) => 
            Console.WriteLine($"[DacPac] {e.Status}: {e.Message}");

        await Task.Run(() => 
            services.Deploy(package, databaseName, upgradeExisting: true, options), ct);

        Console.WriteLine($"Database '{databaseName}' deployed successfully!");
    }

    private static async Task RunProcess(
        string fileName, 
        string[] args, 
        CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        foreach (var arg in args) 
            psi.ArgumentList.Add(arg);

        using var process = System.Diagnostics.Process.Start(psi)!;
        
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"{fileName} failed with exit code {process.ExitCode}\n" +
                $"STDOUT:\n{stdout}\n" +
                $"STDERR:\n{stderr}");
    }
}