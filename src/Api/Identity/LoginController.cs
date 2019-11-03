using DreamTravel.Domain.Users;
using DreamTravel.Features.Identity.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Api.Identity
{
    [AllowAnonymous]
    [EnableCors("dupa")]
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
                _logger.LogInformation($"Attempt to log in with email: [{user.Email}] and password: [{user.Password}]");
                int id = _logging.LogIn(user);

                return Ok(id);
            }

            catch (LoginException ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }
    }
}