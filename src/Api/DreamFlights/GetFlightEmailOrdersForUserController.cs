using System;
using DreamTravel.Features.DreamFlight.GetFlightEmailOrdersForUser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Api.DreamFlights
{
    [Route(Route)]
    public class GetFlightEmailOrdersForUserController : Controller
    {
        public const string Route = "api/GetFlightEmailOrders/{userId}";

        private readonly ILogger<GetFlightEmailOrdersForUserController> _logger;
        private readonly IGetFlightEmailOrdersForUser _getFlightEmailOrdersForUser;


        public GetFlightEmailOrdersForUserController(IGetFlightEmailOrdersForUser getFlightEmailOrdersForUser,
                          ILogger<GetFlightEmailOrdersForUserController> logger)
        {
            _getFlightEmailOrdersForUser = getFlightEmailOrdersForUser;
            _logger = logger;
        }


        [HttpGet]
        public IActionResult GetFlightEmailOrdersForUser([FromRoute] int userId)
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
    }
}
