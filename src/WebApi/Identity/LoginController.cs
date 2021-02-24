using DreamTravel.Domain.Users;
using DreamTravel.Identity.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Api.Identity
{
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
        public IActionResult Login([FromBody] User user)
        {
            _logger.LogInformation($"Attempt to log in with email: [{user.Email}] and password: [{user.Password}]");
            var result = _loginUser.Handle(user);

            if (result.Message != string.Empty)
            {
                _logger.LogError(result.Message);
                return BadRequest(result.Message);
            }

            return Ok(result.User);

        }
    }
}