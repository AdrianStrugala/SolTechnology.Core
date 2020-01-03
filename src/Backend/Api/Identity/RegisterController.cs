using DreamTravel.Domain.Users;
using DreamTravel.Identity.Registration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Api.Identity
{
    [AllowAnonymous]
    [EnableCors("dupa")]
    [Route(Route)]
    public class RegisterController : Controller
    {
        public const string Route = "api/register";

        private readonly ILogger<RegisterController> _logger;
        private readonly IRegisterUser _registerUser;


        public RegisterController(IRegisterUser registerUser, ILogger<RegisterController> logger)
        {
            _registerUser = registerUser;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult Register([FromBody] User user)
        {
            _logger.LogInformation($"Attempt to register user with email: [{user.Email}]");

            try
            {
                _registerUser.Register(user);
                return Ok();
            }

            catch (RegisterException ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }
    }
}