using System.Data;

namespace SolTechnology.Core.SQL.Connections
{
    /// <summary>
    /// Opens SQL Server connections for the configured <see cref="SQLConfiguration.ConnectionString"/>. Registered as
    /// transient by <see cref="ModuleInstaller.AddSolSQL"/>.
    /// </summary>
    public interface ISQLConnectionFactory
    {
        /// <summary>
        /// Returns an open connection that the caller disposes. Transient SQL Server errors (for example a timeout,
        /// a deadlock, or Azure SQL throttling) are retried three times with 3, 9, and 27 second delays; any other
        /// <see cref="Microsoft.Data.SqlClient.SqlException"/> is thrown at once.
        /// </summary>
        IDbConnection CreateConnection();

        /// <summary>
        /// Returns the configured connection string, for example to build an Entity Framework context or a raw
        /// <see cref="Microsoft.Data.SqlClient.SqlConnection"/> without the retry.
        /// </summary>
        string GetConnectionString();
    }
}