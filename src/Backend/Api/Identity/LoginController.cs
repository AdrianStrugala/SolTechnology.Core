using DreamTravel.Domain.Users;
using DreamTravel.Identity.Logging;
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
        private readonly ILoginUser _loginUser;


        public LoginController(ILoginUser loginUser, ILogger<LoginController> logger)
        {
            _loginUser = loginUser;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult OrderFlightEmail([FromBody] User user)
        {
            try
            {
                _logger.LogInformation($"Attempt to log in with email: [{user.Email}] and password: [{user.Password}]");
                User result = _loginUser.Login(user);

                return Ok(result);
            }

            catch (LoginException ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }
    }
}