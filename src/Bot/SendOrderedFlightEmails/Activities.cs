using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.FlightEmailOrders;
using DreamTravel.Features.GetFlightEmailOrders;
using DreamTravel.Features.SendOrderedFlightEmail.Interfaces;
using Microsoft.Azure.WebJobs;

namespace DreamTravel.Bot.SendOrderedFlightEmails
{
    public class Activities
    {
        public const string GetFlightEmailOrdersFunctionName = "GetFlightEmailOrders";
        public const string SendOrderedFlightEmailFunctionName = "SendOrderedFlightEmail";

        private readonly IGetFlightEmailOrders _getFlightEmailOrders;
        private readonly ISendOrderedFlightEmail _sendOrderedFlightEmail;

        public Activities(IGetFlightEmailOrders getFlightEmailOrders, ISendOrderedFlightEmail sendOrderedFlightEmail)
        {
            _getFlightEmailOrders = getFlightEmailOrders;
            _sendOrderedFlightEmail = sendOrderedFlightEmail;
        }

        [FunctionName(GetFlightEmailOrdersFunctionName)]
        public async Task<List<FlightEmailData>> GetFlightEmailOrders(
            [ActivityTrigger] object input)
        {
            return _getFlightEmailOrders.Execute();
        }


        [FunctionName(SendOrderedFlightEmailFunctionName)]
        public async Task SendOrderedFlightEmail(
            [ActivityTrigger] FlightEmailData input)
        {
            _sendOrderedFlightEmail.Execute(input);
        }
    }
}
