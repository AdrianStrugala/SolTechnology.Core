using Microsoft.AspNetCore.Mvc;

namespace DreamTravel.Api
{
    public class HomeController : Controller
    {

        [Route("")]
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("Dream Travels API works!");
        }
    }
}
