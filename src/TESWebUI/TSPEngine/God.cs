using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TESWebUI.Models;

namespace TESWebUI.TSPEngine
{
    class God
    {
        private static ConcurrentBag<int[]> _paths;

        public static int[] SolveTSP(DistanceMatrixEvaluated evaluatedMatrix)
        {
            int noOfCities = (int) Math.Sqrt(evaluatedMatrix.Distances.Length);

            int noOfUniverses = noOfCities * noOfCities * 100;
            _paths = new ConcurrentBag<int[]>();

            Parallel.For(0, noOfUniverses, i => CreateUniverse(noOfCities));

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

        private static int[] FindRandomRoute(int noOfCities)
        {
            List<int> toDraw = new List<int>();


            int[] foundPath = new int[noOfCities];
            for (int i = 0; i < noOfCities; i++)
            {
                foundPath[i] = -1;
            }

            foundPath[0] = 0;


            for (int i = 1; i < noOfCities - 1; i++)
            {
                toDraw.Add(i);
            }

            for (int i = 1; i < noOfCities - 1; i++)
            {
                int draw = toDraw[StaticRandom.Rand(toDraw.Count)];
                foundPath[i] = draw;
                toDraw.Remove(draw);
            }

            foundPath[noOfCities - 1] = noOfCities - 1;

            return foundPath;
        }


        protected static int[] FindMinimumPathInListOfPaths(IEnumerable<int[]> pathList, DistanceMatrixEvaluated distanceMatrix,
            int noOfCities)
        {
            double min = double.MaxValue;
            int[] resultPath = new int[noOfCities];

            foreach (var path in pathList)
            {
                if (path != null)
                {
                    double distance = CalculateDistanceInPath(path, distanceMatrix);
                    if (distance < min)
                    {
                        min = distance;
                        resultPath = path;
                    }
                }
            }

            return resultPath;
        }

        public static double CalculateDistanceInPath(int[] path, DistanceMatrixEvaluated distanceMatrix)
        {
            double result = 0;

            if (path != null)
            {
                for (int i = 0; i < path.Length - 1; i++)
                {
                    result += distanceMatrix.Distances[path[i] * path.Length + path[i + 1]];
                }
            }

            return result;
        }

        public static class StaticRandom
        {
            static int _seed = Environment.TickCount;

            static readonly ThreadLocal<Random> Random =
                new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

            public static int Rand(int max)
            {
                return Random.Value.Next(max);
            }
        }
    }
}
