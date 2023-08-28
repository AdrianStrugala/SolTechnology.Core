using System.Collections.Generic;
using DreamTravel.Domain.Flights;
using DreamTravel.GeolocationData.AzairApi.GetFlights;

namespace DreamTravel.GeolocationData
{
    public interface IAzairApiClient
    {
        List<Flight> GetFlights(GetFlightsQuery query);
    }
}