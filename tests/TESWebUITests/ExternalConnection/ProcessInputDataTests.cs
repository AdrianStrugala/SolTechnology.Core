using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Xunit;

namespace TESWebUITests.ExternalConnection
{
    public class ProcessInputDataTests
    {
        private readonly ProcessInputData _sut; 

        public ProcessInputDataTests()
        {
            ICallAPI apiCaller = new CallAPI();

            _sut = new ProcessInputData(apiCaller);
        }


        [Fact]
        public void DownloadExternalData_ValidConditions_MatrixIsPopulated()
        {
            //Arrange
            City firstCity = new City
            {
                Name = "first",
                Latitude = 51,
                Longitude = 17
            };

            City secondCity = new City
            {
                Name = "second",
                Latitude = 53,
                Longitude = 19
            };

            List<City> cities = new List<City> {firstCity, secondCity};

            EvaluationMatrix matrix = new EvaluationMatrix(2);


            //Act
            _sut.Execute(cities, matrix);


            //Assert
            Assert.Equal(4, matrix.Costs.Length);
            Assert.Equal(4, matrix.FreeDistances.Length);
            Assert.Equal(4, matrix.TollDistances.Length);

            //valid values
            Assert.Equal(double.MaxValue, matrix.FreeDistances[0]);
            Assert.Equal(double.MaxValue, matrix.FreeDistances[3]);
            Assert.NotEqual(double.MaxValue, matrix.FreeDistances[1]);
            Assert.NotEqual(double.MaxValue, matrix.FreeDistances[2]);
        }
    }
}
