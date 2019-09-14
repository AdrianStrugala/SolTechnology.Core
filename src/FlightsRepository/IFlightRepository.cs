using DreamTravel.FlightProviderData.Flights.GetFlights;

namespace DreamTravel.FlightProviderData
{
    public interface IFlightRepository
    {
        GetFlightsResult GetFlights(GetFlightsQuery query);
    }
}