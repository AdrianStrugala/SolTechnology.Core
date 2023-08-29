using System.Threading.Tasks;
using DreamTravel.Identity.ChangePassword;
using DreamTravel.Domain.Users;
using DreamTravel.Identity.Login;
using DreamTravel.Identity.Register;
using DreamTravel.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Api.Identity
{
    public class Users : Controller
    {
        private readonly ILogger<Users> _logger;
        private readonly IQueryHandler<LoginQuery, LoginResult> _loginUser;
        private readonly ICommandHandler<RegisterUserCommand> _registerUserHandler;
        private readonly ICommandHandler<ChangePasswordCommand> _changePassword;


        public Users(
            ICommandHandler<ChangePasswordCommand> changePassword,
            IQueryHandler<LoginQuery, LoginResult> loginUser,
            ICommandHandler<RegisterUserCommand> registerUserHandler,
            ILogger<Users> logger
            )
        {
            _changePassword = changePassword;
            _logger = logger;
            _loginUser = loginUser;
            _registerUserHandler = registerUserHandler;
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
        public async Task<IActionResult> Login([FromBody] LoginQuery query)
        {
            _logger.LogInformation($"Attempt to log in with email: [{query.Email}] and password: [{query.Password}]");
            var result = await _loginUser.Handle(query);

            if (result.Message != string.Empty)
            {
                _logger.LogError(result.Message);
                return BadRequest(result.Message);
            }

            return Ok(result.User);
        }

        [HttpPost]
        [Route("api/users/register")]
        public IActionResult Register([FromBody] RegisterUserCommand command)
        {
            _logger.LogInformation($"Attempt to register user with email: [{command.Email}]");

            var result = _registerUserHandler.Handle(command);

            if (result.Success == false)
            {
                _logger.LogError(result.Message);
                return BadRequest(result.Message);
            }

            return Ok();
        }
    }
}