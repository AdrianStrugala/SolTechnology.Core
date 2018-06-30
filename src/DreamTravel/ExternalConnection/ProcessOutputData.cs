using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public class ProcessOutputData
    {
        public List<Path> FormOutputFromTSFResult(List<City> listOfCities, int[] orderOfCities, IEvaluationMatrix evaluationMatrix)
        {
            int noOfCities = orderOfCities.Length;
            int noOfPaths = noOfCities - 1;
            List<Path> paths = new List<Path>();


            for (int i = 0; i < noOfPaths; i++)
            {
                Path currentPath = new Path
                {
                    StartingCity = listOfCities[orderOfCities[i]],
                    EndingCity = listOfCities[orderOfCities[i + 1]],
                    Cost = evaluationMatrix.OptimalCosts[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    Distance = evaluationMatrix.OptimalDistances[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    Goal = evaluationMatrix.Goals[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    MaxCost = evaluationMatrix.Costs[orderOfCities[i + 1] + orderOfCities[i] * noOfCities]
                };
                paths.Add(currentPath);
            }

            return paths;
        }
    }
}
