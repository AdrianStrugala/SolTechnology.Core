using System.Collections.Generic;
using DreamTravel.Domain.Airports;

namespace DreamTravel.DreamFlights.GetAirports
{
    public interface IGetAirports
    {
        List<Airport> Execute();
    }

    public class GetAirports : IGetAirports
    {
        private readonly IAirportRepository _airportRepository;

        public GetAirports(IAirportRepository airportRepository)
        {
            _airportRepository = airportRepository;
        }

        public List<Airport> Execute()
        {
            return _airportRepository.Get();
        }
    }
}
