using System.Collections.Generic;

namespace DreamTravel.FlightData
{
    public interface IAirportRepository
    {
        List<string> GetCodes();
        Dictionary<string, List<string>> GetPlaceToCodesMap();
    }
}