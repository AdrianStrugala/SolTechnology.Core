namespace DreamTravel.Bot.DiscoverIndividualChances.DataAccess
{
    using Interfaces;
    using Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class GetFlightsFromSkyScanner : IGetFlightsFromSkyScanner
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "prtl6749387986743898559646983194";

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

            string fromLocationRequest =
                $"http://partners.api.skyscanner.net/apiservices/autosuggest/v1.0/PL-sky/{subscription.Currency}/pl-PL?query={subscription.From}&apiKey={ApiKey}&fbclid=IwAR3YcSivV9V769LNrXU6TuVhDFpY3BE4RZHBFUXMQm4sOU5Lfm1MqdCS25Y";

            string locationResponse = await _httpClient.GetStringAsync(fromLocationRequest);
            JObject fromLocationJson = JObject.Parse(locationResponse);
            var fromId = fromLocationJson["Places"][0]["PlaceId"].Value<string>();


            string toLocationRequest =
                $"http://partners.api.skyscanner.net/apiservices/autosuggest/v1.0/PL-sky/EUR/pl-PL?query={subscription.To}&apiKey={ApiKey}&fbclid=IwAR3YcSivV9V769LNrXU6TuVhDFpY3BE4RZHBFUXMQm4sOU5Lfm1MqdCS25Y";

            string toLocationResponse = await _httpClient.GetStringAsync(toLocationRequest);
            JObject toLocationJson = JObject.Parse(toLocationResponse);
            var toId = toLocationJson["Places"][0]["PlaceId"].Value<string>();


            string travelRequest =
                $"http://partners.api.skyscanner.net/apiservices/browsequotes/v1.0/PL-sky/EUR/pl-PL/{fromId}/{toId}/anytime/anytime?apiKey={ApiKey}&fbclid=IwAR3YcSivV9V769LNrXU6TuVhDFpY3BE4RZHBFUXMQm4sOU5Lfm1MqdCS25Y";

            string travelResponse = await _httpClient.GetStringAsync(travelRequest);
            JObject travelJson = JObject.Parse(travelResponse);

            result.Price = double.MaxValue;
            int minIndex = -1;


            for (int i = 0; i < travelJson["Quotes"].Count(); i++)
            {
                //if price lower than current min
                if (travelJson["Quotes"][i]["MinPrice"].Value<double>() < result.Price)
                {
                    //if length of stay in range specified by user
                    if (travelJson["Quotes"][i]["InboundLeg"]["DepartureDate"].Value<DateTime>()
                            .Subtract(travelJson["Quotes"][i]["OutboundLeg"]["DepartureDate"].Value<DateTime>())
                        < new TimeSpan(subscription.LengthOfStay, 0, 0, 0))
                    {
                        result.Price = travelJson["Quotes"][i]["MinPrice"].Value<double>();
                        minIndex = i;
                    }
                }
            }

            var bestQuote = travelJson["Quotes"][minIndex];


            string therePlaceId = bestQuote["OutboundLeg"]["OriginId"].Value<string>();
            string backPlaceId = bestQuote["InboundLeg"]["OriginId"].Value<string>();
            foreach (var place in travelJson["Places"])
            {
                if (place["PlaceId"].Value<string>() == backPlaceId)
                {
                    result.Destination = place["Name"].Value<string>();
                }

                if (place["PlaceId"].Value<string>() == therePlaceId)
                {
                    result.Origin = place["Name"].Value<string>();
                }
            }


            string thereCarrierId = bestQuote["OutboundLeg"]["CarrierIds"][0].Value<string>();
            string backCarrierId = bestQuote["InboundLeg"]["CarrierIds"][0].Value<string>();
            foreach (var carrier in travelJson["Carriers"])
            {
                if (carrier["CarrierId"].Value<string>() == backCarrierId)
                {
                    result.BackCarrier = carrier["Name"].Value<string>();
                }

                if (carrier["CarrierId"].Value<string>() == thereCarrierId)
                {
                    result.ThereCarrier = carrier["Name"].Value<string>();
                }
            }

            result.ActualAt = bestQuote["QuoteDateTime"].Value<string>();
            result.ThereDay = bestQuote["OutboundLeg"]["DepartureDate"].Value<string>().Substring(0, 10);
            result.BackDay = bestQuote["InboundLeg"]["DepartureDate"].Value<string>().Substring(0, 10);

            return result;
        }
    }
}
