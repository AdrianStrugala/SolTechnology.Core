using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TravelingSalesmanProblem
{
    using System.Collections.Generic;

    public class God : TSPAbstract
    {
        private static ConcurrentBag<List<int>> _paths;
        private const int MaxNoOfUniverses = 1000000;

        public override List<int> SolveTSP(List<double> distances)
        {
            int noOfCities = (int)Math.Sqrt(distances.Count);

            int Factorial(int x) => x <= 1 ? 1 : x * Factorial(x - 1);
            int noOfCitiesFactorial = Factorial(noOfCities);
            int noOfUniverses = MaxNoOfUniverses;
            if (noOfCitiesFactorial < MaxNoOfUniverses / 10)
            {
                noOfUniverses = noOfCitiesFactorial * 10;
            }

            _paths = new ConcurrentBag<List<int>>();

            Parallel.For(0, noOfUniverses,
                i => CreateUniverse(noOfCities));

            var minimumPath = FindMinimumPathInListOfPaths(_paths, distances, noOfCities);

            return minimumPath;
        }

        private static void CreateUniverse(int noOfCities)
        {
            List<int> randomRoute = FindRandomRoute(noOfCities);
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