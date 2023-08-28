using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Airports;
using DreamTravel.Infrastructure;

namespace DreamTravel.DreamFlights.GetAirports
{
    public class GetAirportsHandler : IQueryHandler<GetAirportsQuery, List<Airport>>
    {
        private readonly IAirportRepository _airportRepository;

        public GetAirportsHandler(IAirportRepository airportRepository)
        {
            _airportRepository = airportRepository;
        }

        public Task<List<Airport>> Handle(GetAirportsQuery query)
        {
            return Task.FromResult(_airportRepository.GetAll());
        }
    }
}