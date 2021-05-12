using DreamTravel.DreamFlights;
using DreamTravel.Infrastructure.Configuration;
using DreamTravel.Infrastructure.Database;
using DreamTravel.Infrastructure.Email;

namespace DreamTravel.Api.Configuration
{
    public static partial class ConfigurationResolver
    {
        public static ApplicationConfiguration GetConfiguration(string environmentName)
        {
            if (environmentName?.ToLower() == "demo")
            {
                return GetDemoConfiguration();
            }

            return GetLocalConfiguration();
        }

        private static ApplicationConfiguration GetLocalConfiguration()
        {
            var config = new ApplicationConfiguration
            {
                SqlDatabaseConfiguration = new SqlDatabaseConfiguration
                {
                    ConnectionString = "Data Source=localhost,1401;Database=DreamTravelDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
                },
                DreamFlightsConfiguration = new DreamFlightsConfiguration
                {
                    SendEmails = false
                },
                ApiConfiguration = new ApiConfiguration
                {
                    ApiName = "dreamTravels-local"
                }
            };

            return config;
        }
    }


}