using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DreamTravel.FlightProviderData.Airports
{
    public partial class AirportRepository : IAirportRepository
    {
        public List<string> GetCodesByPlace(string place)
        {
            var placeToAirportsDataModels = JsonConvert.DeserializeObject<Dictionary<string, PlaceToAirportsDataModel>>(Place2CodesMap);

            List<string> placeToCodesMap = placeToAirportsDataModels.Values.Single(p => p.Name.Equals(place)).Ports.Split("_").ToList();

            return placeToCodesMap;
        }
    }
}