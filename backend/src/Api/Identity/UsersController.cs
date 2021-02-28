using DreamTravel.Identity.ChangePassword;
using DreamTravel.Identity.Logging;
using DreamTravel.Identity.Registration;
using DreamTravel.Domain.Users;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Api.Identity
{
    public class Users : Controller
    {
        private readonly ILogger<Users> _logger;
        private readonly ILoginUser _loginUser;
        private readonly IRegisterUser _registerUser;
        private readonly IChangePassword _changePassword;


        public Users(IChangePassword changePassword, ILogger<Users> logger, ILoginUser loginUser, IRegisterUser registerUser)
        {
            _changePassword = changePassword;
            _logger = logger;
            _loginUser = loginUser;
            _registerUser = registerUser;
        }


        [HttpPost]
        [Route("api/users/changePassword")]
        public IActionResult ChangePassword([FromBody] ChangePasswordCommand command)
        {
            _logger.LogInformation($"Changing password for user: [{command.UserId}]");
            var result = _changePassword.Handle(command);

            if (result.Success == false)
            {
                _logger.LogError(result.Message);
                return BadRequest(result.Message);
            }

            return Ok();
        }

        [HttpPost]
        [Route("api/users/login")]
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

        [HttpPost]
        [Route("api/users/register")]
        public IActionResult Register([FromBody] User user)
        {
            _logger.LogInformation($"Attempt to register user with email: [{user.Email}]");

            var result = _registerUser.Handle(user);

            if (result.Success == false)
            {
                _logger.LogError(result.Message);
                return BadRequest(result.Message);
            }

            return Ok();
        }
    }
}