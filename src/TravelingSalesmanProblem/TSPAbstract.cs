using System;
using System.Collections.Generic;
using System.Threading;
using TravelingSalesmanProblem.Models;

namespace TravelingSalesmanProblem
{
    public abstract class TSPAbstract
    {
        public abstract int[] SolveTSP(IDistanceMatrix distanceMatrix);

        protected static int[] FindRandomRoute(int noOfCities)
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

        protected static int[] FindMinimumPathInListOfPaths(IEnumerable<int[]> pathList, IDistanceMatrix distanceMatrix,
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

        public static double CalculateDistanceInPath(int[] path, IDistanceMatrix distanceMatrix)
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

        protected static class StaticRandom
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
