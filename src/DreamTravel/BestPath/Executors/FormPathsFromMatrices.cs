namespace DreamTravel.BestPath.Executors
{
    using Interfaces;
    using SharedModels;
    using System.Collections.Generic;
    using System.Linq;

    public class FormPathsFromMatrices : IFormOutputData
    {
        public List<Path> Execute(List<City> listOfCities, IEvaluationMatrix evaluationMatrix, List<int> orderOfCities = null)
        {
            if (orderOfCities == null)
            {
                orderOfCities = Enumerable.Range(0, listOfCities.Count).ToList();
            }

            int noOfCities = orderOfCities.Count;
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

            foreach (var path in paths)
            {
                if (path.VinietaCost == 0) continue;
                double vinietaCost = path.VinietaCost;
                var pathsUsingThisVinieta = paths.Where(x => x.VinietaCost.Equals(vinietaCost)).ToList();

                foreach (var pathOnThisVinieta in pathsUsingThisVinieta)
                {
                    pathOnThisVinieta.Cost += vinietaCost / pathsUsingThisVinieta.Count / pathsUsingThisVinieta.Count;
                }
            }

            return paths;
        }
    }
}
