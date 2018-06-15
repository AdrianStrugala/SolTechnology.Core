using System.Collections.Generic;
using TravelingSalesmanProblem.Models;

namespace DreamTravel.ExternalConnection
{
    public class ProcessOutputData
    {
        public List<Path> FormOutputFromTSFResult(List<City> listOfCities, int[] orderOfCities, IDistanceMatrix distanceMatrix)
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
                    Cost = distanceMatrix.Costs[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    Distance = distanceMatrix.Distances[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    Goal = distanceMatrix.Goals[orderOfCities[i + 1] + orderOfCities[i] * noOfCities]
                };
                paths.Add(currentPath);
            }

            return paths;
        }
    }
}
