namespace DreamTravel.Infrastructure
{
    public class ApplicationConfiguration
    {
        private static string _connectionString;

        public string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value;
        }        
    }
}
