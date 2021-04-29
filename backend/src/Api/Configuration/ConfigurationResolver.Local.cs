using DreamTravel.DatabaseData.Configuration;
using DreamTravel.Infrastructure.Database;
using Microsoft.Extensions.Options;

namespace DreamTravel.Api.Configuration
{
    public static partial class ConfigurationResolver
    {
        public static ApplicationConfiguration GetConfiguration(string environmentName)
        {
            // if (_applicationConfiguration.Environment == "Local" || _applicationConfiguration.Environment == "")
            // {
            //     return GetLocalConfiguration();
            // }

            return GetLocalConfiguration();
        }

        private static ApplicationConfiguration GetLocalConfiguration()
        {
            var config = new ApplicationConfiguration
            {
                SqlDatabaseConfiguration = new SqlDatabaseConfiguration
                {
                    ConnectionString = "Data Source=localhost,1401;Database=DreamTravelDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
                }
            };

            return config;
        }
    }
}