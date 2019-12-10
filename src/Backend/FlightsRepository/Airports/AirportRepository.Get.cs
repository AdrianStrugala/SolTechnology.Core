using System.Collections.Generic;
using DreamTravel.Domain.Airports;

namespace DreamTravel.FlightProviderData.Airports
{
    public partial class AirportRepository : IAirportRepository
    {
        public List<Airport> Get()
        {
            List<Airport> result = new List<Airport>();

            Dictionary<string, List<string>> countryToCodes = GetCountryToCodesMap();
            Dictionary<string, string> cityToCodes = GetCityToCodeMap();


            foreach (KeyValuePair<string, string> cityToCode in cityToCodes)
            {
                result.Add(new Airport
                {
                    Name = cityToCode.Key,
                    Codes = new List<string> { cityToCode.Value }
                });
            }

            foreach (KeyValuePair<string, List<string>> countryToCode in countryToCodes)
            {
                string name = countryToCode.Key;
                if (cityToCodes.ContainsKey(name))
                {
                    name += " (All Airports)";
                }

                result.Add(new Airport
                {
                    Name = name,
                    Codes = countryToCode.Value
                });
            }

            return result;
        }
    }
}