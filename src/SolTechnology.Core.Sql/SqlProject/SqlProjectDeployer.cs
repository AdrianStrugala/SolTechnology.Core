using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;

namespace SolTechnology.Core.Sql.SqlProject;

public static class SqlProjectDeployer
{
    public static async Task DeployDacpacAsync(
        string dacpacPath,
        string connectionString, 
        string databaseName,
        CancellationToken ct = default)
    {
        if (!File.Exists(dacpacPath))
            throw new FileNotFoundException($"Dacpac not found: {dacpacPath}");

        Console.WriteLine($"🔧 Deploying dacpac: {dacpacPath}");

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

        Console.WriteLine($"✅ Database '{databaseName}' deployed successfully!");
    }
}