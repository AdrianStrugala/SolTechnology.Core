using Microsoft.AspNetCore.Mvc;

namespace TESWebUI.Models
{
    public class TESViewModel
    {
        public string ListOfCities { get; set; }

        public string CalculateBestPath()
        {
            return ("Como; Verona; Milan");
        }
    }
}