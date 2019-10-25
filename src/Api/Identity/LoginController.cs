using System;
using DreamTravel.Domain.Users;
using DreamTravel.Features.Identity.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Api.Identity
{
    [AllowAnonymous]

    [Route(Route)]
    public class LoginController : Controller
    {
        public const string Route = "api/login";

        private readonly ILogger<LoginController> _logger;
        private readonly ILogging _logging;


        public LoginController(ILogging logging, ILogger<LoginController> logger)
        {
            _logging = logging;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult OrderFlightEmail([FromBody] User user)
        {
            try
            {
                int id = _logging.LogIn(user);

                return Ok(id);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }
    }
}