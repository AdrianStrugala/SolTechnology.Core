using Microsoft.AspNetCore.Mvc;

namespace SolTechnology.TaleCode.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class HealthController : ControllerBase
    {
        [HttpGet(Name = "GetHealth")]
        public string GetAsync()
        {
            return "I'm Alive!";
        }
    }
}