using System.Collections.Generic;

namespace DreamTravel.Domain.Airports
{
    public interface IAirportRepository
    {
        List<string> GetCodes();
        Dictionary<string, List<string>> GetPlaceToCodesMap();
    }
}