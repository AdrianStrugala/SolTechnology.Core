using System.Collections.Generic;

namespace DreamTravel.FlightProviderData
{
    public interface IAirportRepository
    {
        List<string> GetCodes();
        Dictionary<string, List<string>> GetPlaceToCodesMap();
        List<string> GetCodesByCountry(string country);
    }
}