using System;
using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetPreviewUsers;
using DreamTravel.Domain.Flights;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Models;
using DreamTravel.FlightProviderData.Query.GetFlights;
using DreamTravel.Infrastructure.Email;

namespace DreamTravel.DreamFlights.SendDreamTravelFlightEmail
{
    public class SendDreamTravelFlightEmail : ISendDreamTravelFlightEmail
    {
        private readonly IComposeMessage _composeMessage;
        private readonly IGetPreviewUsers _getPreviewUsers;
        private readonly IGetFlights _getFlights;
        private readonly IFilterFlights _filterFlights;

        public SendDreamTravelFlightEmail(IComposeMessage composeMessage, IGetPreviewUsers getPreviewUsers, IGetFlights getFlights, IFilterFlights filterFlights)
        {
            _composeMessage = composeMessage;
            _getPreviewUsers = getPreviewUsers;
            _getFlights = getFlights;
            _filterFlights = filterFlights;
        }

        public void Execute()
        {
            GetFlightsQuery getFlightsQuery = new GetFlightsQuery
            (
                new KeyValuePair<string, List<string>>("Wroclaw", new List<string> { "WRO" }),
                new KeyValuePair<string, List<string>>("Anywhere", new List<string> { "XXX" }),
                DateTime.UtcNow,
                DateTime.UtcNow.AddMonths(3),
                2,
                5
            );

            List<Flight> flights = _getFlights.Execute(getFlightsQuery);
            flights = _filterFlights.Execute(flights);

            var users = _getPreviewUsers.Execute();

            foreach (var user in users)
            {
                string message = _composeMessage.ExecuteHtml(flights, user.Name);
                EmailAgent.Send(new DreamTravelChanceEmail(message, user.Email));
            }
        }
    }
}
