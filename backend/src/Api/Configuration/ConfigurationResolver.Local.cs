using DreamTravel.DreamFlights;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.Api.Configuration
{
    public static partial class ConfigurationResolver
    {
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
                }
            };

            return config;
        }
    }
}