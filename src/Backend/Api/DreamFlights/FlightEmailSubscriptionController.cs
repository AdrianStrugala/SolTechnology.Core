using System;
using DreamTravel.Domain.FlightEmailOrders;
using DreamTravel.DreamFlights.DeleteFlightEmailOrder;
using DreamTravel.DreamFlights.GetFlightEmailOrdersForUser;
using DreamTravel.DreamFlights.OrderFlightEmail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamFlights
{
    [AllowAnonymous]
    public class FlightEmailSubscriptionController : Controller
    {
        public const string DeleteRoute = "api/FlightEmailSubscription/{id}";
        public const string GetByUserIdRoute = "api/FlightEmailSubscription/{userId}";
        public const string InsertRoute = "api/FlightEmailSubscription";

        private readonly ILogger<FlightEmailSubscriptionController> _logger;
        private readonly IDeleteFlightEmailOrder _deleteFlightEmailOrder;
        private readonly IGetFlightEmailOrdersForUser _getFlightEmailOrdersForUser;
        private readonly IOrderFlightEmail _orderFlightEmail;


        public FlightEmailSubscriptionController(
            IDeleteFlightEmailOrder deleteFlightEmailOrder,
            IGetFlightEmailOrdersForUser getFlightEmailOrdersForUser,
            IOrderFlightEmail orderFlightEmail,
            ILogger<FlightEmailSubscriptionController> logger)
        {
            _deleteFlightEmailOrder = deleteFlightEmailOrder;
            _getFlightEmailOrdersForUser = getFlightEmailOrdersForUser;
            _orderFlightEmail = orderFlightEmail;
            _logger = logger;
        }


        [HttpDelete]
        [Route(DeleteRoute)]
        public IActionResult Delete([FromRoute] int id)
        {
            _logger.LogInformation($"Deleting flight email subscription, [{id}]");
            
            try
            {
                _deleteFlightEmailOrder.Execute(id);

                return Ok();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                string message = JsonConvert.SerializeObject(ex.Message);
                return BadRequest(message);
            }
        }

        [HttpGet]
        [Route(GetByUserIdRoute)]
        public IActionResult GetByUser([FromRoute] int userId)
        {
            try
            {
                _logger.LogInformation($"Getting flight email orders for user: [{userId}]");

                var result = _getFlightEmailOrdersForUser.Execute(userId);

                return Ok(result);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route(InsertRoute)]
        public IActionResult Insert([FromBody] FlightEmailOrder flightEmailOrder)
        {
            try
            {
                flightEmailOrder.ArrivalDate = flightEmailOrder.ArrivalDate.ToUniversalTime();
                flightEmailOrder.DepartureDate = flightEmailOrder.DepartureDate.ToUniversalTime();

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
