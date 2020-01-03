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
            GetFlightsOrder getFlightsOrder = new GetFlightsOrder
            (
                new KeyValuePair<string, List<string>>(flightEmailOrder.From, _airportRepository.GetByPlace(flightEmailOrder.From).Codes),
                new KeyValuePair<string, List<string>>(flightEmailOrder.To, _airportRepository.GetByPlace(flightEmailOrder.To).Codes),
                flightEmailOrder.DepartureDate,
                flightEmailOrder.ArrivalDate,
                flightEmailOrder.MinDaysOfStay,
                flightEmailOrder.MaxDaysOfStay
                );

            List<Flight> flights = _flightRepository.GetFlights(getFlightsOrder);

            string message = _composeMessage.Execute(flights, flightEmailOrder);
            EmailAgent.Send(new OrderedFlightEmail(
                message,
                flightEmailOrder.Email,
                $"{flightEmailOrder.UserName} choose your flight to {flightEmailOrder.To}!"));
        }
    }
}