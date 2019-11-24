using System.Collections.Generic;
using DreamTravel.Domain.Airports;
using DreamTravel.Domain.FlightEmailOrders;
using DreamTravel.Domain.Flights;
using DreamTravel.Domain.Flights.GetFlights;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Models;
using DreamTravel.Infrastructure.Email;

namespace DreamTravel.DreamFlights.SendOrderedFlightEmail
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

        public void Execute(FlightEmailData flightEmailOrder)
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

            string message = _composeMessage.Execute(flights, flightEmailOrder);
            EmailAgent.Send(new OrderedFlightEmail(message,
                                                   flightEmailOrder.Email,
                                $"{flightEmailOrder.UserName} choose your flight to {flightEmailOrder.To}!"));
        }
    }
}