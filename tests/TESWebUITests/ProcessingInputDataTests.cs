using System;
using System.Collections.Generic;
using TESWebUI.Models;
using Xunit;

namespace TESWebUITests
{
    public class ProcessingInputDataTests
    {
        [Fact]
        public void GetCostBetweenTwoCities()
        {
            City firstCity = new City
            {
                Name = "first",
                Latitude = 30,
                Longitude = 30
            };

            City secondCity = new City
            {
                Name = "second",
                Latitude = 50,
                Longitude = 50
            };
            
            var result = TESWebUI.TSPEngine.ProcessInputData.GetCostBetweenTwoCities(firstCity, secondCity);

            Assert.NotEqual(0,result);
        }
    }
}
