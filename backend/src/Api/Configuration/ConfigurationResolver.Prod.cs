using DreamTravel.DreamFlights;
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
                    ConnectionString = "Server=tcp:dreamtravel.database.windows.net,1433;Initial Catalog=dreamtravel;Persist Security Info=False;User ID=adrian;Password=P4ssw0rd@;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"
                },
                DreamFlightsConfiguration = new DreamFlightsConfiguration
                {
                    SendEmails = true
                }
            };

            return config;
        }
    }
}