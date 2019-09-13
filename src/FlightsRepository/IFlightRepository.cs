using DreamTravel.FlightData.Flights.GetFlights;

namespace DreamTravel.FlightData
{
    public interface IFlightRepository
    {
        GetFlightsResult GetFlights(GetFlightsQuery query);
    }
}