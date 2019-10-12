using System;
using DreamTravel.Domain.FlightEmailOrders;
using DreamTravel.Features.OrderFlightEmail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.DreamFlights
{
    [AllowAnonymous]
    
    [Route(Route)]
    public class OrderFlightEmailController : Controller
    {
        public const string Route = "api/OrderFlightEmail";

        private readonly ILogger<OrderFlightEmailController> _logger;
        private readonly IOrderFlightEmail _orderFlightEmail;


        public OrderFlightEmailController(IOrderFlightEmail orderFlightEmail,
                          ILogger<OrderFlightEmailController> logger)
        {
            _orderFlightEmail = orderFlightEmail;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult OrderFlightEmail([FromBody] FlightEmailOrder flightEmailOrder)
        {
            try
            {
                _logger.LogInformation($"Ordering flight email for user: [{flightEmailOrder.UserId}]");

                _orderFlightEmail.Execute(flightEmailOrder);

                return Ok();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }
    }
}
