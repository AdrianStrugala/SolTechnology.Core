using System.Collections.Generic;
using DreamTravel.Domain.Flights;

namespace DreamTravel.FlightProviderData.Query.GetFlights
{
    public interface IGetFlights
    {
        List<Flight> Execute(GetFlightsQuery query);
    }
}