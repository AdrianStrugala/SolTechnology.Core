//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using DreamTravel.Domain.Cities;
//
//namespace DreamTravel.GeolocationData.Matrices
//{
//    public partial class MatrixRepository : IMatrixRepository
//    {
//        public Task<(double[], double[])> GetCosts(List<City> cities)
//        {
//            int matrixSize = cities.Count * cities.Count;
//
//            var result = (new double[matrixSize], new double[matrixSize]);
//
//            List<Task> tasks = new List<Task>();
//
//            for (int i = 0; i < matrixSize; i++)
//            {
//                for (int j = 0; j < matrixSize; j++)
//                {
//                    int iterator = j + i * matrixSize;
//
//                    var i1 = i;
//                    var j1 = j;
//                    tasks.Add(Task.Run(async () => (evaluationMatrix.Costs[iterator], evaluationMatrix.VinietaCosts[iterator]) =
//                                                   await _downloadCostBetweenTwoCities.Execute(cities[i1], cities[j1])));
//                }
//            }
//
//            return tasks;
//        }
//
//        private async Task<(double, double)> DownloadCostBetweenTwoCities(City origin, City destination)
//        {
//
//        }
//
//    }
//}
