using DreamTravel.DreamFlights;
using DreamTravel.Infrastructure.Configuration;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.Api.Configuration
{
    public static partial class ConfigurationResolver
    {
        private static ApplicationConfiguration GetDemoConfiguration()
        {
            var config = new ApplicationConfiguration
            {
                SqlDatabaseConfiguration = new SqlDatabaseConfiguration
                {
                    ConnectionString = "Server=tcp:dreamtravel.database.windows.net,1433;Initial Catalog=dreamtravel-demo;Persist Security Info=False;User ID=adrian;Password=P4ssw0rd@;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"
                },
                DreamFlightsConfiguration = new DreamFlightsConfiguration
                {
                    SendEmails = false
                },
                ApiConfiguration = new ApiConfiguration
                {
                    ApiName = "dreamTravels-demo"
                }
            };

            return config;
        }
    }
}