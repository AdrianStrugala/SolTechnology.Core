using Microsoft.AspNetCore.Mvc;

namespace SolTechnology.TaleCode.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetHealth")]
        public string GetAsync()
        {
            _logger.LogWarning("Run");
            return "I'm the Tale COde Api!";
        }
    }
}