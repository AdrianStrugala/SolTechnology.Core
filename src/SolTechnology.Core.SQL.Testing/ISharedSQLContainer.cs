using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace SolTechnology.Core.SQL.Testing;

/// <summary>
/// Public contract exposing a running SQL Server container and its shared docker network so a sibling
/// fixture — notably <c>SolTechnology.Core.ServiceBus.Testing</c> (the emulator needs a backing MSSQL)
/// — can reuse the same instance instead of spawning a second. The emulator MUST use its own catalog;
/// <see cref="SQLFixture"/>'s reset is scoped to the application catalog only.
/// </summary>
public interface ISharedSQLContainer
{
    /// <summary>The running SQL Server container.</summary>
    IContainer Container { get; }

    /// <summary>The docker network the container is attached to, if any.</summary>
    INetwork? Network { get; }

    /// <summary>Server-level (no catalog) admin connection string.</summary>
    string ServerConnectionString { get; }

    /// <summary>Connection string targeting an arbitrary <paramref name="databaseName"/> on the shared instance.</summary>
    string GetDatabaseConnectionString(string databaseName);
}


