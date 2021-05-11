using DreamTravel.DatabaseData.Configuration;
using DreamTravel.DreamFlights;
using DreamTravel.Infrastructure.Database;
using DreamTravel.Infrastructure.Email;

namespace DreamTravel.Api.Configuration
{
    public class ApplicationConfiguration
    {
        public SqlDatabaseConfiguration SqlDatabaseConfiguration { get; set; }

        public DreamFlightsConfiguration DreamFlightsConfiguration { get; set; }
    }
}