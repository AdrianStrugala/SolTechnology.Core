using System.Collections.Generic;
using DreamTravel.BestPath.Interfaces;
using DreamTravel.SharedModels;

namespace DreamTravel.BestPath
{
    public class FormOutputData : IFormOutputData
    {
        public List<Path> Execute(List<City> listOfCities, int[] orderOfCities, IEvaluationMatrix evaluationMatrix)
        {
            int noOfCities = orderOfCities.Length;
            int noOfPaths = noOfCities - 1;
            List<Path> paths = new List<Path>();


            for (int i = 0; i < noOfPaths; i++)
            {
                Path currentPath = new Path
                {
                    Index = i,
                    StartingCity = listOfCities[orderOfCities[i]],
                    EndingCity = listOfCities[orderOfCities[i + 1]],
                    OptimalCost = evaluationMatrix.OptimalCosts[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    VinietaCost = evaluationMatrix.VinietaCosts[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    OptimalDistance = evaluationMatrix.OptimalDistances[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    Goal = evaluationMatrix.Goals[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    Cost = evaluationMatrix.Costs[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    FreeDistance = evaluationMatrix.FreeDistances[orderOfCities[i + 1] + orderOfCities[i] * noOfCities],
                    TollDistance = evaluationMatrix.TollDistances[orderOfCities[i + 1] + orderOfCities[i] * noOfCities]
                };
                paths.Add(currentPath);
            }

            //increase cost of each road using vinieta (divide by number of roads using this vinieta)

            return paths;
        }
    }
}
