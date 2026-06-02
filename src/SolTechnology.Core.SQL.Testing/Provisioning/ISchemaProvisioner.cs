namespace SolTechnology.Core.SQL.Testing.Provisioning;

/// <summary>
/// Strategy for getting the application schema into a freshly-started container. Dacpac, raw scripts
/// and a generic delegate (where EF migrations plug in) are the shipped implementations.
/// </summary>
internal interface ISchemaProvisioner
{
    /// <summary>
    /// <see langword="true"/> when the provisioner creates the application database itself (e.g. dacpac
    /// with <c>CreateNewDatabase</c>). When <see langword="false"/>, the fixture must ensure the catalog
    /// exists before provisioning (MSSQL scripts / delegate on a fresh container that has only master).
    /// </summary>
    bool CreatesDatabase { get; }

    Task ProvisionAsync(SQLFixture fixture, CancellationToken ct);
}


