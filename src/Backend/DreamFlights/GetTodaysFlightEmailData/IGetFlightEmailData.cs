using System.Collections.Generic;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DreamFlights.GetFlightEmailData
{
    public interface IGetFlightEmailData
    {
        List<FlightEmailData> Execute();
    }
}