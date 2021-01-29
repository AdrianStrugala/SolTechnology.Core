using System;
using System.Collections.Generic;
using DreamTravel.DatabaseData.Query.GetPreviewUsers;
using DreamTravel.Domain.Flights;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Interfaces;
using DreamTravel.DreamFlights.SendDreamTravelFlightEmail.Models;
using DreamTravel.GeolocationData;
using DreamTravel.GeolocationData.AzairApi.GetFlights;
using DreamTravel.Infrastructure.Email;

namespace DreamTravel.DreamFlights.SendDreamTravelFlightEmail
{
    public class SendDreamTravelFlightEmailHandler : ISendDreamTravelFlightEmail
    {
        private readonly IComposeMessage _composeMessage;
        private readonly IGetPreviewUsers _getPreviewUsers;
        private readonly IAzairApiClient _azairApiClient;
        private readonly IFilterFlights _filterFlights;

        public SendDreamTravelFlightEmailHandler(IComposeMessage composeMessage, IGetPreviewUsers getPreviewUsers, IAzairApiClient azairApiClient, IFilterFlights filterFlights)
        {
            _composeMessage = composeMessage;
            _getPreviewUsers = getPreviewUsers;
            _azairApiClient = azairApiClient;
            _filterFlights = filterFlights;
        }

        public void Handle()
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

            List<Flight> flights = _azairApiClient.GetFlights(getFlightsQuery);
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
