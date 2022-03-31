using Microsoft.AspNetCore.Mvc;

namespace SolTechnology.TaleCode.EventListener
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
            return $"My name is {nameof(EventListener)}. I'll become the king of Message Bus!";
        }
    }
}