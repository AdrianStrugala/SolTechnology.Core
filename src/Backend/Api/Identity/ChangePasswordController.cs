using DreamTravel.Identity.ChangePassword;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Api.Identity
{
    [Route(Route)]
    public class ChangePasswordController : Controller
    {
        public const string Route = "api/changePassword";

        private readonly ILogger<ChangePasswordController> _logger;
        private readonly IChangePassword _changePassword;


        public ChangePasswordController(IChangePassword changePassword, ILogger<ChangePasswordController> logger)
        {
            _changePassword = changePassword;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult Login([FromBody] ChangePasswordCommand command)
        {
            try
            {
                _logger.LogInformation($"Changing password for user: [{command.UserId}]");
                _changePassword.Execute(command);

                return Ok();
            }

            catch (ChangePasswordException ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }
    }
}