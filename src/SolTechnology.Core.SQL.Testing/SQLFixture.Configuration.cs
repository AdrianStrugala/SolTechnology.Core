using DotNet.Testcontainers.Networks;
using SolTechnology.Core.SQL.Testing.Provisioning;

namespace SolTechnology.Core.SQL.Testing;

public sealed partial class SQLFixture
{
    private ISchemaProvisioner? _provisioner;
    private INetwork? _network;
    private string? _networkAlias;

    /// <summary>Use PostgreSQL instead of the default SQL Server. Call before <see cref="Initialize"/>.</summary>
    public SQLFixture UsePostgres()
    {
        Provider = SQLProvider.Postgres;
        return this;
    }

    /// <summary>Provision the schema by building and deploying a dacpac from a <c>.sqlproj</c> (SQL Server only).</summary>
    public SQLFixture WithSQLProject(string sqlProjPath)
    {
        _provisioner = new DacpacProvisioner(sqlProjPath);
        return this;
    }

    /// <summary>Provision the schema by executing raw <c>.sql</c> script files, in order.</summary>
    public SQLFixture WithScripts(params string[] scriptPaths)
    {
        _provisioner = new ScriptProvisioner(scriptPaths);
        return this;
    }

    /// <summary>
    /// Provision the schema with a delegate that receives the application database connection string.
    /// This is the EF-migrations seam: the package stays EF-free; the consumer runs migrations with
    /// their own <c>DbContext</c>.
    /// </summary>
    public SQLFixture WithEfMigrations(Func<string, Task> applyMigrations)
    {
        ArgumentNullException.ThrowIfNull(applyMigrations);
        _provisioner = new DelegateProvisioner((cs, _) => applyMigrations(cs));
        return this;
    }

    /// <summary>General provisioning seam: a delegate receiving the application connection string and a token.</summary>
    public SQLFixture WithSchema(Func<string, CancellationToken, Task> provision)
    {
        _provisioner = new DelegateProvisioner(provision);
        return this;
    }

    /// <summary>Attach the container to a docker network (used to share MSSQL with the Service Bus emulator).</summary>
    public SQLFixture WithNetwork(INetwork network, string? alias = null)
    {
        _network = network;
        _networkAlias = alias;
        return this;
    }
}
