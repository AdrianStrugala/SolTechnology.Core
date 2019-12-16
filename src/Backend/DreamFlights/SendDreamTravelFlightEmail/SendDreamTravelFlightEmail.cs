using System;
using System.Collections.Generic;
using DreamTravel.Domain.Flights;
using DreamTravel.Domain.Flights.GetFlights;
using DreamTravel.Domain.Users;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Models;
using DreamTravel.Infrastructure.Email;

namespace DreamTravel.DreamFlights.SendDreamTravelFlightEmail
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
            GetFlightsOrder getFlightsOrder = new GetFlightsOrder
            (
                new KeyValuePair<string, List<string>>("Wroclaw", new List<string> { "WRO" }),
                new KeyValuePair<string, List<string>>("Anywhere", new List<string> { "XXX" }),
                DateTime.UtcNow,
                DateTime.UtcNow.AddMonths(3),
                2,
                5
            );

            List<Flight> flights = _flightRepository.GetFlights(getFlightsOrder);

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
