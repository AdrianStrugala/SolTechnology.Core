namespace DreamTravel.Bot.DiscoverIndividualChances.DataAccess
{
    using Interfaces;
    using Models;
    using Newtonsoft.Json.Linq;
    using SharedModels;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class GetFlightsFromSkyScanner : IGetFlightsFromSkyScanner
    {
        private readonly HttpClient _httpClient;
        private const string APIKey = "prtl6749387986743898559646983194";

        public GetFlightsFromSkyScanner()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }
        }
        public async Task<Chance> Execute(Subscription subscription)
        {
            Chance result = new Chance();
            string fromId = null;
            string toId = null;

            string fromLocationRequest =
                $"http://partners.api.skyscanner.net/apiservices/autosuggest/v1.0/PL-sky/EUR/pl-PL?query={subscription.From}&apiKey={APIKey}&fbclid=IwAR3YcSivV9V769LNrXU6TuVhDFpY3BE4RZHBFUXMQm4sOU5Lfm1MqdCS25Y";


            string fromLocationResposne = await _httpClient.GetStringAsync(fromLocationRequest);
            JObject fromLocationJson = JObject.Parse(fromLocationResposne);
            fromId = fromLocationJson["Places"][0]["PlaceId"].Value<string>();


            string toLocationRequest =
                $"http://partners.api.skyscanner.net/apiservices/autosuggest/v1.0/PL-sky/EUR/pl-PL?query={subscription.To}&apiKey={APIKey}&fbclid=IwAR3YcSivV9V769LNrXU6TuVhDFpY3BE4RZHBFUXMQm4sOU5Lfm1MqdCS25Y";

            string toLocationResposne = await _httpClient.GetStringAsync(toLocationRequest);
            JObject toLocationJson = JObject.Parse(toLocationResposne);
            toId = toLocationJson["Places"][0]["PlaceId"].Value<string>();


            string travelRequest =
                $"http://partners.api.skyscanner.net/apiservices/BrowseDates/v1.0/PL-sky/EUR/pl-PL/{fromId}/{toId}/2019-03/2019-03?apiKey={APIKey}&fbclid=IwAR3YcSivV9V769LNrXU6TuVhDFpY3BE4RZHBFUXMQm4sOU5Lfm1MqdCS25Y";

            string travelResposne = await _httpClient.GetStringAsync(travelRequest);
            JObject travelJson = JObject.Parse(travelResposne);
          //  toId = toLocationJson["Places"][0]["PlaceId"].Value<string>();

            return result;
        }
    }
}
