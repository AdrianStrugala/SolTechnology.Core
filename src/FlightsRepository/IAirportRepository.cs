using System.Collections.Generic;

namespace DreamTravel.FlightProviderData
{
    public interface IAirportRepository
    {
        Dictionary<string, string> GetCityToCodeMap();
        Dictionary<string, List<string>> GetCountryToCodesMap();
        List<string> GetCodesByPlace(string place);
    }
}