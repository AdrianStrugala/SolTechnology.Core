using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.DreamFlights.GetFlightEmailData;
using DreamTravel.DreamFlights.SendOrderedFlightEmail.Interfaces;
using Microsoft.Azure.WebJobs;

namespace DreamTravel.Bot.SendOrderedFlightEmails
{
    public class Activities
    {
        public const string GetFlightEmailOrdersFunctionName = "GetFlightEmailOrders";
        public const string SendOrderedFlightEmailFunctionName = "SendOrderedFlightEmail";

        private readonly IGetFlightEmailData _getFlightEmailData;
        private readonly ISendOrderedFlightEmail _sendOrderedFlightEmail;

        public Activities(IGetFlightEmailData getFlightEmailData, ISendOrderedFlightEmail sendOrderedFlightEmail)
        {
            _getFlightEmailData = getFlightEmailData;
            _sendOrderedFlightEmail = sendOrderedFlightEmail;
        }

        [FunctionName(GetFlightEmailOrdersFunctionName)]
        public async Task<List<FlightEmailData>> GetFlightEmailOrders(
            [ActivityTrigger] object input)
        {
            return _getFlightEmailData.Execute();
        }


        [FunctionName(SendOrderedFlightEmailFunctionName)]
        public async Task SendOrderedFlightEmail(
            [ActivityTrigger] FlightEmailData input)
        {
            _sendOrderedFlightEmail.Execute(input);
        }
    }
}
