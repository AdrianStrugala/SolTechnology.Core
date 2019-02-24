namespace DreamTravel.Bot.DiscoverIndividualChances.DataAccess
{
    using Interfaces;
    using Models;
    using Newtonsoft.Json.Linq;
    using System.Linq;
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


            string locationResponse = await _httpClient.GetStringAsync(fromLocationRequest);
            JObject fromLocationJson = JObject.Parse(locationResponse);
            fromId = fromLocationJson["Places"][0]["PlaceId"].Value<string>();


            string toLocationRequest =
                $"http://partners.api.skyscanner.net/apiservices/autosuggest/v1.0/PL-sky/EUR/pl-PL?query={subscription.To}&apiKey={APIKey}&fbclid=IwAR3YcSivV9V769LNrXU6TuVhDFpY3BE4RZHBFUXMQm4sOU5Lfm1MqdCS25Y";

            string toLocationResposne = await _httpClient.GetStringAsync(toLocationRequest);
            JObject toLocationJson = JObject.Parse(toLocationResposne);
            toId = toLocationJson["Places"][0]["PlaceId"].Value<string>();


            string travelRequest =
                $"http://partners.api.skyscanner.net/apiservices/browsequotes/v1.0/PL-sky/EUR/pl-PL/{fromId}/{toId}/anytime/anytime?apiKey={APIKey}&fbclid=IwAR3YcSivV9V769LNrXU6TuVhDFpY3BE4RZHBFUXMQm4sOU5Lfm1MqdCS25Y";

            string travelResposne = await _httpClient.GetStringAsync(travelRequest);
            JObject travelJson = JObject.Parse(travelResposne);

            result.Price = double.MaxValue;
            int minIndex = -1;


            for (int i = 0; i < travelJson["Quotes"].Count(); i++)
            {
                if (travelJson["Quotes"][i]["MinPrice"].Value<double>() < result.Price)
                {
                    result.Price = travelJson["Quotes"][i]["MinPrice"].Value<double>();
                    minIndex = i;
                }
            }

            var bestQuote = travelJson["Quotes"][minIndex];

            
            result.Origin = subscription.From;
            result.Destination = subscription.To;
            result.ActualAt = bestQuote["QuoteDateTime"].Value<string>();

            string thereCarrierId = bestQuote["OutboundLeg"]["CarrierIds"][0].Value<string>();
            foreach (var carrier in travelJson["Carriers"])
            {
                if (carrier["CarrierId"].Value<string>() == thereCarrierId)
                {
                    result.ThereCarrier = carrier["Name"].Value<string>();
                    break;
                }
            }

            string backCarrierId = bestQuote["InboundLeg"]["CarrierIds"][0].Value<string>();
            foreach (var carrier in travelJson["Carriers"])
            {
                if (carrier["CarrierId"].Value<string>() == backCarrierId)
                {
                    result.BackCarrier = carrier["Name"].Value<string>();
                }
            }

            result.ThereDay = bestQuote["OutboundLeg"]["DepartureDate"].Value<string>().Substring(0,10);
            result.BackDay = bestQuote["InboundLeg"]["DepartureDate"].Value<string>().Substring(0, 10);

            return result;
        }
    }
}
