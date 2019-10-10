using System.Collections.Generic;

namespace DreamTravel.FlightProviderData.Airports
{
    public partial class AirportRepository : IAirportRepository
    {
        public List<string> GetCodesByPlace(string place)
        {
            Dictionary<string, List<string>> countryToCodes = GetCountryToCodesMap();
            Dictionary<string, string> cityToCodes = GetCityToCodeMap();

            var combinedResult = countryToCodes;

            foreach (KeyValuePair<string, string> cityToCode in cityToCodes)
            {
                if (!combinedResult.TryAdd(cityToCode.Key, new List<string>
                {
                    cityToCode.Value
                }))
                {
                    combinedResult.Add($"{cityToCode.Key} ({cityToCode.Value})", new List<string>
                    {
                        cityToCode.Value
                    });
                };
            }

            return combinedResult[place];
        }
    }
}