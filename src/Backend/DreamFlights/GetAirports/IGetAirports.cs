using System.Collections.Generic;
using DreamTravel.Domain.Airports;

namespace DreamTravel.DreamFlights.GetAirports
{
    public interface IGetAirports
    {
        List<Airport> Handle();
    }
}
