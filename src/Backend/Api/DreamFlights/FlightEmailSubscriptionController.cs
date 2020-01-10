using System;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.DreamFlights.DeleteFlightEmailSubscription;
using DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser;
using DreamTravel.DreamFlights.SubscribeForFlightEmail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamFlights
{
    public class FlightEmailSubscriptionController : Controller
    {
        public const string DeleteRoute = "api/FlightEmailSubscription/{id}";
        public const string GetByUserIdRoute = "api/FlightEmailSubscription/{userId}";
        public const string InsertRoute = "api/FlightEmailSubscription";

        private readonly ILogger<FlightEmailSubscriptionController> _logger;
        private readonly IDeleteFlightEmailSubscription _deleteFlightEmailSubscription;
        private readonly IGetFlightEmailSubscriptionsForUser _getFlightEmailSubscriptionsForUser;
        private readonly ISubscribeForFlightEmail _subscribeForFlightEmail;


        public FlightEmailSubscriptionController(
            IDeleteFlightEmailSubscription deleteFlightEmailSubscription,
            IGetFlightEmailSubscriptionsForUser getFlightEmailSubscriptionsForUser,
            ISubscribeForFlightEmail subscribeForFlightEmail,
            ILogger<FlightEmailSubscriptionController> logger)
        {
            _deleteFlightEmailSubscription = deleteFlightEmailSubscription;
            _getFlightEmailSubscriptionsForUser = getFlightEmailSubscriptionsForUser;
            _subscribeForFlightEmail = subscribeForFlightEmail;
            _logger = logger;
        }


        [HttpDelete]
        [Route(DeleteRoute)]
        public IActionResult Delete([FromRoute] int id)
        {
            _logger.LogInformation($"Deleting flight email subscription, [{id}]");
            
            try
            {
                _deleteFlightEmailSubscription.Execute(id);

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

                var result = _getFlightEmailSubscriptionsForUser.Execute(userId);

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
        public IActionResult Insert([FromBody] FlightEmailSubscription flightEmailSubscription)
        {
            try
            {
                flightEmailSubscription.ArrivalDate = flightEmailSubscription.ArrivalDate.ToUniversalTime();
                flightEmailSubscription.DepartureDate = flightEmailSubscription.DepartureDate.ToUniversalTime();

                _logger.LogInformation($"Ordering flight email for user: [{flightEmailSubscription.UserId}]");

                _subscribeForFlightEmail.Execute(flightEmailSubscription);

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
