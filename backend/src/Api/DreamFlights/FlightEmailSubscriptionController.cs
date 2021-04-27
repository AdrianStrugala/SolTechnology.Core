using System;
using System.Windows.Input;
using DreamTravel.DatabaseData.Query.GetSubscriptionsWithDays;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.DreamFlights.DeleteFlightEmailSubscription;
using DreamTravel.DreamFlights.GetFlightEmailSubscriptionsForUser;
using DreamTravel.DreamFlights.SubscribeForFlightEmail;
using DreamTravel.DreamFlights.UpdateSubscriptions;
using DreamTravel.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DreamTravel.Api.DreamFlights
{
    public class FlightEmailSubscriptionController : Controller
    {
        public const string DeleteRoute = "api/FlightEmailSubscription/{id}";
        public const string GetByUserIdRoute = "api/FlightEmailSubscription/{userId}";
        public const string UpdateListRoute = "api/FlightEmailSubscription/{userId}";
        public const string InsertRoute = "api/FlightEmailSubscription";

        private readonly ILogger<FlightEmailSubscriptionController> _logger;
        private readonly ICommandHandler<DeleteFlightEmailSubscriptionCommand> _deleteFlightEmailSubscription;
        private readonly IGetFlightEmailSubscriptionsForUser _getFlightEmailSubscriptionsForUser;
        private readonly ICommandHandler<SubscribeForFlightEmailsCommand> _subscribeForFlightEmail;
        private readonly ICommandHandler<UpdateSubscriptionsCommand> _updateSubscriptions;


        public FlightEmailSubscriptionController(
            ICommandHandler<DeleteFlightEmailSubscriptionCommand> deleteFlightEmailSubscription,
            IGetFlightEmailSubscriptionsForUser getFlightEmailSubscriptionsForUser,
            ICommandHandler<SubscribeForFlightEmailsCommand> subscribeForFlightEmail,
            ICommandHandler<UpdateSubscriptionsCommand> updateSubscriptions,
            ILogger<FlightEmailSubscriptionController> logger)
        {
            _deleteFlightEmailSubscription = deleteFlightEmailSubscription;
            _getFlightEmailSubscriptionsForUser = getFlightEmailSubscriptionsForUser;
            _subscribeForFlightEmail = subscribeForFlightEmail;
            _updateSubscriptions = updateSubscriptions;
            _logger = logger;
        }


        [HttpDelete]
        [Route(DeleteRoute)]
        public IActionResult Delete([FromRoute] int id)
        {
            _logger.LogInformation($"Deleting flight email subscription, [{id}]");

            try
            {
                _deleteFlightEmailSubscription.Handle(new DeleteFlightEmailSubscriptionCommand{Id = id});

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

                var result = _getFlightEmailSubscriptionsForUser.Handle(new GetSubscriptionsWithDaysQuery(userId));

                return Ok(result.SubscriptionsWithDays);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route(UpdateListRoute)]
        public IActionResult UpdateList([FromBody] UpdateSubscriptionsCommand request)
        {
            try
            {
                _logger.LogInformation($"Updating subscriptions for user: [{request.UserId}]");

                _updateSubscriptions.Handle(request);

                return Ok();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route(InsertRoute)]
        public IActionResult Insert([FromBody] SubscribeForFlightEmailsCommand request)
        {
            try
            {
                request.FlightEmailSubscription.ArrivalDate = request.FlightEmailSubscription.ArrivalDate.ToUniversalTime();
                request.FlightEmailSubscription.DepartureDate = request.FlightEmailSubscription.DepartureDate.ToUniversalTime();

                _logger.LogInformation($"Ordering flight email for user: [{request.FlightEmailSubscription.UserId}]");

                _subscribeForFlightEmail.Handle(request);

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
