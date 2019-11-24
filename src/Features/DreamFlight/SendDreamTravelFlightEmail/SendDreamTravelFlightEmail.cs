using System;
using System.Collections.Generic;
using DreamTravel.DatabaseData;
using DreamTravel.Domain.Flights;
using DreamTravel.Domain.Flights.GetFlights;
using DreamTravel.Domain.Users;
using DreamTravel.Features.DreamFlight.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.Features.DreamFlight.SendDreamTravelFlightEmail.Models;
using DreamTravel.FlightProviderData;
using DreamTravel.FlightProviderData.Flights.GetFlights;
using DreamTravel.Infrastructure.Email;

namespace DreamTravel.Features.DreamFlight.SendDreamTravelFlightEmail
{
    public class SendDreamTravelFlightEmail : ISendDreamTravelFlightEmail
    {
        private readonly IComposeMessage _composeMessage;
        private readonly IUserRepository _userRepository;
        private readonly IFlightRepository _flightRepository;
        private readonly IFilterFlights _filterFlights;

        public SendDreamTravelFlightEmail(IComposeMessage composeMessage, IUserRepository userRepository, IFlightRepository flightRepository, IFilterFlights filterFlights)
        {
            _composeMessage = composeMessage;
            _userRepository = userRepository;
            _flightRepository = flightRepository;
            _filterFlights = filterFlights;
        }

        public void Execute()
        {
            GetFlightsQuery getFlightsQuery = new GetFlightsQuery
            {
                ArrivalDate = DateTime.UtcNow.AddMonths(3),
                DepartureDate = DateTime.UtcNow,
                Departures = new KeyValuePair<string, List<string>>("Wroclaw", new List<string> {"WRO"}),
                Arrivals = new KeyValuePair<string, List<string>>("Anywhere", new List<string> {"XXX"}),
                MinDaysToStay = 2,
                MaxDaysToStay = 5
            };

            List<Flight> flights = _flightRepository.GetFlights(getFlightsQuery).Flights;

            flights = _filterFlights.Execute(flights);

            var users = _userRepository.GetPreviewUsers();

            foreach (var user in users)
            {
                string message = _composeMessage.ExecuteHtml(flights, user.Name);
                EmailAgent.Send(new DreamTravelChanceEmail(message, user.Email));
            }
        }
    }
}
