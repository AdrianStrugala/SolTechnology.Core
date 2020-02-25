using System.Collections.Generic;
using DreamTravel.Domain.Airports;

namespace DreamTravel.FlightProviderData.Airports
{
    public partial class AirportRepository : IAirportRepository
    {
        public List<Airport> GetAll()
        {
            return AirportDataSource.Get();
        }
    }
}