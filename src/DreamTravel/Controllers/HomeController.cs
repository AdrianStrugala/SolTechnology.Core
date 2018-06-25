using System.Collections.Generic;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Microsoft.AspNetCore.Mvc;

namespace DreamTravel.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
