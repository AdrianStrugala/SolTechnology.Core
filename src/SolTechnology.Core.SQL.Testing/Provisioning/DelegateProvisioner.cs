namespace SolTechnology.Core.SQL.Testing.Provisioning;

/// <summary>
/// Generic provisioner: hands the application database connection string to a consumer-supplied
/// delegate. This is the seam through which <b>EF Core migrations</b> are applied — the package stays
/// ORM-agnostic and EF-free; the consumer constructs their own <c>DbContext</c> and calls
/// <c>Database.MigrateAsync()</c>.
/// </summary>
internal sealed class DelegateProvisioner(Func<string, CancellationToken, Task> provision) : ISchemaProvisioner
{
    public bool CreatesDatabase => false;

    public Task ProvisionAsync(SQLFixture fixture, CancellationToken ct) =>
        provision(fixture.DatabaseConnectionString, ct);
}


