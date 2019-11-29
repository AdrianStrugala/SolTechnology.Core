using System;
using System.Collections.Generic;
using System.Net;
using DreamTravel.Domain.Flights;
using DreamTravel.Domain.Flights.GetFlights;
using HtmlAgilityPack;

namespace DreamTravel.FlightProviderData.Flights.GetFlights
{
    public class FlightRepository : IFlightRepository
    {
        public GetFlightsResult GetFlights(GetFlightsQuery query)
        {
            GetFlightsResult result = new GetFlightsResult();

            DateTime departureDate = query.DepartureDate > DateTime.UtcNow ? query.DepartureDate : DateTime.UtcNow;

            string url = $"http://www.azair.eu/azfin.php?searchtype=flexi" +
                         $"&tp=0&isOneway=return" +
                         $"&srcAirport={query.Departures.Key}+{FormattedStringProvider.Airports(query.Departures.Value)}" +
                         "&srcap1=POZ" +
                         "&srcFreeAirport=" +
                         $"&srcTypedText={query.Departures.Key}" +
                         "&srcFreeTypedText=" +
                         "&srcMC=" +
                         $"&dstAirport={query.Arrivals.Key}+{FormattedStringProvider.Airports(query.Arrivals.Value)}" +
                         "&dstap10=PMI&dstFreeAirport=" +
                         $"&dstTypedText={query.Arrivals.Key}" +
                         "&dstFreeTypedText=" +
                         "&dstMC=" +
                         $"&depmonth={FormattedStringProvider.Month(departureDate)}" +
                         $"&depdate={FormattedStringProvider.Date(departureDate)}" +
                         "&aid=0" +
                         $"&arrmonth={FormattedStringProvider.Month(query.ArrivalDate)}" +
                         $"&arrdate={FormattedStringProvider.Date(query.ArrivalDate)}" +
                         $"&minDaysStay={query.MinDaysToStay}" +
                         $"&maxDaysStay={query.MaxDaysToStay}" +
                         "&dep0=true" +
                         "&dep1=true" +
                         "&dep2=true" +
                         "&dep3=true" +
                         "&dep4=true" +
                         "&dep5=true" +
                         "&dep6=true" +
                         "&arr0=true" +
                         "&arr1=true" +
                         "&arr2=true" +
                         "&arr3=true" +
                         "&arr4=true" +
                         "&arr5=true" +
                         "&arr6=true" +
                         "&samedep=true" +
                         "&samearr=true" +
                         "&minHourStay=0%3A45" +
                         "&maxHourStay=23%3A20" +
                         "&minHourOutbound=0%3A00" +
                         "&maxHourOutbound=24%3A00" +
                         "&minHourInbound=0%3A00" +
                         "&maxHourInbound=24%3A00" +
                         "&autoprice=true" +
                         "&adults=2&children=0" +
                         "&infants=0" +
                         "&maxChng=0" +
                         "&currency=EUR" +
                         "&indexSubmit=Search";

            List<Flight> flights = new List<Flight>();

            using (WebClient client = new WebClient())
            {
                string html = client.DownloadString(url + ".html");

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                //price
                var priceNodes = htmlDoc.DocumentNode.SelectNodes("//span[contains(@class, 'tp')]");
                foreach (var priceNode in priceNodes)
                {
                    var newChance = new Flight();

                    //trim the currency symbol
                    var stringPrice = priceNode.InnerHtml.Remove(0, 1);
                    newChance.Price = double.Parse(stringPrice);

                    flights.Add(newChance);
                }

                //date
                var dateNodes = htmlDoc.DocumentNode.SelectNodes("//span[contains(@class, 'date')]");
                for (int i = 0; i < dateNodes.Count - 1; i += 2)
                {
                    flights[i / 2].ThereDate = dateNodes[i].InnerHtml;
                    flights[i / 2].BackDate = dateNodes[i + 1].InnerHtml;
                }

                //from
                var fromNodes = htmlDoc.DocumentNode.SelectNodes("//span[contains(@class, 'from')]");
                for (int i = 0; i < fromNodes.Count - 3; i += 4)
                {
                    flights[i / 4].ThereDepartureCity = fromNodes[i].InnerHtml.Remove(0, 23).Split("<")[0];
                    flights[i / 4].ThereDepartureHour = fromNodes[i].InnerHtml.Remove(0, 8).Remove(5);


                    flights[i / 4].BackDepartureCity = fromNodes[i + 2].InnerHtml.Remove(0, 23).Split("<")[0];
                    flights[i / 4].BackDepartureHour = fromNodes[i + 2].InnerHtml.Remove(0, 8).Remove(5);
                }

                //to
                var toNodes = htmlDoc.DocumentNode.SelectNodes("//span[contains(@class, 'to')]");
                for (int i = 0; i < toNodes.Count - 5; i += 6)
                {
                    flights[i / 6].ThereArrivalCity = toNodes[i].InnerHtml.Remove(0, 6).Split("<")[0];
                    flights[i / 6].ThereArrivalHour = toNodes[i].InnerHtml.Remove(5);


                    flights[i / 6].BackArrivalCity = toNodes[i + 3].InnerHtml.Remove(0, 6).Split("<")[0];
                    flights[i / 6].BackArrivalHour = toNodes[i + 3].InnerHtml.Remove(5);
                }
            }

            result.Flights = flights;

            return result;
        }
    }
}
