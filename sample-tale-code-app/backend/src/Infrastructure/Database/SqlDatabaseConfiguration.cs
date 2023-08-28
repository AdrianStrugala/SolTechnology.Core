namespace DreamTravel.Infrastructure.Database
{
    public interface ISqlDatabaseConfiguration
    {
        string ConnectionString { get; set; }
    }

    public class SqlDatabaseConfiguration : ISqlDatabaseConfiguration
    {
        public string ConnectionString { get; set; }
    }
}
