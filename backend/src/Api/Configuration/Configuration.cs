using DreamTravel.DreamFlights;
using DreamTravel.Infrastructure.Configuration;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.Api.Configuration
{
    public class ApplicationConfiguration
    {
        public SqlDatabaseConfiguration SqlDatabaseConfiguration { get; set; }

        public DreamFlightsConfiguration DreamFlightsConfiguration { get; set; }

        public ApiConfiguration ApiConfiguration{ get; set; }
    }
}