using DreamTravel.Domain.Flights.GetFlights;

namespace DreamTravel.Domain.Flights
{
    public interface IFlightRepository
    {
        GetFlightsResult GetFlights(GetFlightsQuery query);
    }
}