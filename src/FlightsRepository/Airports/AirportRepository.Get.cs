using System.Collections.Generic;
using DreamTravel.Domain.Airports;

namespace DreamTravel.FlightProviderData.Airports
{
    public partial class AirportRepository : IAirportRepository
    {
        public List<Airport> Get()
        {
            List<Airport> result = new List<Airport>();
            int i = 0;

            Dictionary<string, List<string>> countryToCodes = GetCountryToCodesMap();
            Dictionary<string, string> cityToCodes = GetCityToCodeMap();

            foreach (KeyValuePair<string, List<string>> countryToCode in countryToCodes)
            {
                result.Add(new Airport
                {
                    Id = i,
                    Name = countryToCode.Key,
                    Codes = countryToCode.Value
                });
                i++;
            }

            foreach (KeyValuePair<string, string> cityToCode in cityToCodes)
            {
                result.Add(new Airport
                {
                    Id = i,
                    Name = cityToCode.Key,
                    Codes = new List<string> { cityToCode.Value }
                });
                i++;
            }

            return result;
        }
    }
}