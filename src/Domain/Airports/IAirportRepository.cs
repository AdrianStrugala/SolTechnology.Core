using System.Collections.Generic;

namespace DreamTravel.Domain.Airports
{
    public interface IAirportRepository
    {
        Dictionary<string, string> GetCityToCodeMap();
        Dictionary<string, List<string>> GetCountryToCodesMap();
        List<string> GetCodesByPlace(string place);
    }
}