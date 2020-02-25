using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetSubscriptionDetailsByDay;
using DreamTravel.Domain.Airports;
using DreamTravel.Domain.FlightEmailSubscriptions;
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

        public void Execute(FlightEmailData flightEmailData)
        {
            GetFlightsOrder getFlightsOrder = new GetFlightsOrder
            (
                new KeyValuePair<string, List<string>>(flightEmailData.From, _airportRepository.GetByPlace(flightEmailData.From).Codes),
                new KeyValuePair<string, List<string>>(flightEmailData.To, _airportRepository.GetByPlace(flightEmailData.To).Codes),
                flightEmailData.DepartureDate,
                flightEmailData.ArrivalDate,
                flightEmailData.MinDaysOfStay,
                flightEmailData.MaxDaysOfStay
                );

            List<Flight> flights = _flightRepository.GetFlights(getFlightsOrder);

            string message = _composeMessage.Execute(flights, flightEmailData);
            EmailAgent.Send(new OrderedFlightEmail(
                message,
                flightEmailData.Email,
                $"{flightEmailData.UserName} choose your flight to {flightEmailData.To}!"));
        }
    }
}