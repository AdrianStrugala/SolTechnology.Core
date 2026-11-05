namespace SolTechnology.Core.SQL
{
    /// <summary>
    /// Settings for <see cref="ModuleInstaller.AddSolSQL"/>.
    /// </summary>
    public class SQLConfiguration
    {
        /// <summary>
        /// SQL Server connection string used by every connection the module opens. Must not be empty; the host fails at
        /// start otherwise.
        /// </summary>
        public required string ConnectionString { get; set; }
    }
}
