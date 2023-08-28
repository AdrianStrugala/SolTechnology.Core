using DreamTravel.Infrastructure.Database;

namespace DreamTravel.Api.Configuration
{
    public static partial class ConfigurationResolver
    {
        private static ApplicationConfiguration GetProdConfiguration()
        {
            var config = new ApplicationConfiguration
            {
                SqlDatabaseConfiguration = new SqlDatabaseConfiguration
                {
                    ConnectionString = ""
                }
            };

            return config;
        }
    }
}
