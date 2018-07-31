using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TravelingSalesmanProblem
{
    public class God : TSPAbstract
    {
        private static ConcurrentBag<int[]> _paths;
        private const int MaxNoOfUniverses = 1000000;

        public override int[] SolveTSP(double[] distances)
        {
            int noOfCities = (int) Math.Sqrt(distances.Length);

            int Factorial(int x) => x <= 1 ? 1 : x * Factorial(x - 1);
            int noOfCitiesFactorial = Factorial(noOfCities);
            int noOfUniverses = MaxNoOfUniverses;
            if (noOfCitiesFactorial < MaxNoOfUniverses/10)
            {
                noOfUniverses = noOfCitiesFactorial * 10;
            }

            _paths = new ConcurrentBag<int[]>();
            

            Parallel.For(0, noOfUniverses, 
                i => CreateUniverse(noOfCities));

            var minimumPath = FindMinimumPathInListOfPaths(_paths, distances, noOfCities);

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