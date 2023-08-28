using System.Collections.Generic;
using DreamTravel.Domain.Airports;

namespace DreamTravel.FlightProviderData.Repository.Airports.PreCalculation
{
    public partial class AirportDataSource
    {
        private static readonly List<Airport> Airports = new List<Airport>();

        public static List<Airport> Get()
        {
            if (Airports.Count > 0)
            {
                return Airports;
            }

            Dictionary<string, List<string>> countryToCodes = GetCountryToCodesMap();
            Dictionary<string, string> cityToCodes = GetCityToCodeMap();


            foreach (KeyValuePair<string, string> cityToCode in cityToCodes)
            {
                Airports.Add(new Airport
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

                Airports.Add(new Airport
                {
                    Name = name,
                    Codes = countryToCode.Value
                });
            }

            return Airports;
        }
    }
}
