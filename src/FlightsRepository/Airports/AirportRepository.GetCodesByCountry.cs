using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DreamTravel.FlightProviderData.Airports
{
    public partial class AirportRepository
    {
        public List<string> GetCodesByCountry(string country)
        {
            var placeToAirportsDataModels = JsonConvert.DeserializeObject<Dictionary<string, PlaceToAirportsDataModel>>(Place2CodesMap);

            List<string> placeToCodesMap = placeToAirportsDataModels.Values.Single(p => p.Name.Equals(country)).Ports.Split("_").ToList();

            return placeToCodesMap;
        }
    }
}