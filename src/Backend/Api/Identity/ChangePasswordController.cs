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
        public IActionResult ChangePassword([FromBody] ChangePasswordCommand command)
        {
            _logger.LogInformation($"Changing password for user: [{command.UserId}]");
            var result = _changePassword.Execute(command);

            if (result.Success == false)
            {
                _logger.LogError(result.Message);
                return BadRequest(result.Message);
            }

            return Ok();
        }
    }
}