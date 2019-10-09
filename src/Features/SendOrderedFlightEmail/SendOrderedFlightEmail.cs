using System.Collections.Generic;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.Domain.Flights;
using DreamTravel.Features.SendOrderedFlightEmail.Interfaces;
using DreamTravel.Features.SendOrderedFlightEmail.Models;
using DreamTravel.FlightProviderData;
using DreamTravel.FlightProviderData.Flights.GetFlights;
using DreamTravel.Infrastructure.Email;

namespace DreamTravel.Features.SendOrderedFlightEmail
{
    public class SendOrderedFlightEmail : ISendOrderedFlightEmail
    {
        private readonly IFlightRepository _flightRepository;
        private readonly IAirportRepository _airportRepository;
        private readonly IComposeMessage _composeMessage;

        public SendOrderedFlightEmail(IFlightRepository flightRepository, IAirportRepository airportRepository, IComposeMessage composeMessage)
        {
            _flightRepository = flightRepository;
            _airportRepository = airportRepository;
            _composeMessage = composeMessage;
        }

        public void Execute(FlightEmailOrder flightEmailOrder)
        {
            GetFlightsQuery getFlightsQuery = new GetFlightsQuery
            {
                ArrivalDate = flightEmailOrder.ArrivalDate,
                DepartureDate = flightEmailOrder.DepartureDate,
                Departures = new KeyValuePair<string, List<string>>(flightEmailOrder.From, _airportRepository.GetCodesByCountry(flightEmailOrder.From)),
                Arrivals = new KeyValuePair<string, List<string>>(flightEmailOrder.To, _airportRepository.GetCodesByCountry(flightEmailOrder.To)),
                MinDaysToStay = flightEmailOrder.MinDaysOfStay,
                MaxDaysToStay = flightEmailOrder.MaxDaysOfStay
            };

            List<Flight> flights = _flightRepository.GetFlights(getFlightsQuery).Flights;

            string message = _composeMessage.Execute(flights, flightEmailOrder);
            EmailAgent.Send(new OrderedFlightEmail(message,
                                                   flightEmailOrder.Email,
                                $"{flightEmailOrder.UserName} choose your flight to {flightEmailOrder.To}!"));
        }
    }
}