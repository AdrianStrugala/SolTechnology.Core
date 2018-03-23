using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TESWebUI.Models;

namespace TESWebUI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CalculateBestPath()
        {
            return Content("Torino; Verona; Milan");
        }


        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
