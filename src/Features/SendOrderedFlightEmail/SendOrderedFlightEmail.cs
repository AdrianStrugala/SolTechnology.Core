using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.Domain.Flights;
using DreamTravel.FlightProviderData;
using DreamTravel.FlightProviderData.Flights.GetFlights;

namespace DreamTravel.Features.SendOrderedFlightEmail
{
    public class SendOrderedFlightEmail : ISendOrderedFlightEmail
    {
        private readonly IFlightRepository _flightRepository;
        private readonly IAirportRepository _airportRepository;

        public SendOrderedFlightEmail(IFlightRepository flightRepository, IAirportRepository airportRepository)
        {
            _flightRepository = flightRepository;
            _airportRepository = airportRepository;
        }

        public void Execute(FlightEmailOrder flightEmailOrder)
        {
            GetFlightsQuery getFlightsQuery = new GetFlightsQuery
            {
                ArrivalDate = flightEmailOrder.ArrivalDate,
                DepartureDate = flightEmailOrder.DepartureDate,
                Departures = new KeyValuePair<string, List<string>>(flightEmailOrder.From, _airportRepository.GetCodesByPlace(flightEmailOrder.From)),
                Arrivals = new KeyValuePair<string, List<string>>(flightEmailOrder.To, _airportRepository.GetCodesByPlace(flightEmailOrder.To)),
                MinDaysToStay = flightEmailOrder.MinDaysOfStay,
                MaxDaysToStay = flightEmailOrder.MaxDaysOfStay
            };

            List<Flight> flights = _flightRepository.GetFlights(getFlightsQuery).Flights;
        }
    }
}