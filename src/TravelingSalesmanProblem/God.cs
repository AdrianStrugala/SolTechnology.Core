using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TravelingSalesmanProblem.Models;

namespace TravelingSalesmanProblem
{
    public class God : TSPAbstract
    {
        private static ConcurrentBag<int[]> _paths;
        private const int NoOfUniverses = 500000;

        public override int[] SolveTSP(IDistanceMatrix evaluatedMatrix)
        {
            int noOfCities = (int) Math.Sqrt(evaluatedMatrix.Distances.Length);

            _paths = new ConcurrentBag<int[]>();

            Parallel.For(0, NoOfUniverses, i => CreateUniverse(noOfCities));

            var minimumPath = FindMinimumPathInListOfPaths(_paths, evaluatedMatrix, noOfCities);

            return minimumPath;
        }

        private static void CreateUniverse(int noOfCities)
        {
            int[] randomRoute = FindRandomRoute(noOfCities);
            try
            {
                _paths.Add(randomRoute);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}