using System.Collections.Generic;
using DreamTravel.Domain.Airports;
using DreamTravel.FlightProviderData.Repository.Airports.PreCalculation;

namespace DreamTravel.FlightProviderData.Repository.Airports
{
    public partial class AirportRepository : IAirportRepository
    {
        public List<Airport> GetAll()
        {
            return AirportDataSource.Get();
        }
    }
}