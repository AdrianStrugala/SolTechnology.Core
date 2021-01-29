using System.Collections.Generic;
using DreamTravel.Domain.Airports;

namespace DreamTravel.DreamFlights.GetAirports
{
    public class GetAirportsHandler : IGetAirports
    {
        private readonly IAirportRepository _airportRepository;

        public GetAirportsHandler(IAirportRepository airportRepository)
        {
            _airportRepository = airportRepository;
        }

        public List<Airport> Handle()
        {
            return _airportRepository.GetAll();
        }
    }
}