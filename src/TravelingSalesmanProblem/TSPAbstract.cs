using System;
using System.Collections.Generic;
using System.Threading;

namespace TravelingSalesmanProblem
{
    public abstract class TSPAbstract : ITSP
    {
        public abstract List<int> SolveTSP(List<double> distances);

        protected static List<int> FindRandomRoute(int noOfCities)
        {
            List<int> toDraw = new List<int>();
            List<int> foundPath = new List<int>(noOfCities);
            foundPath.Add(0);

            for (int i = 1; i < noOfCities - 1; i++)
            {
                toDraw.Add(i);
            }

            for (int i = 1; i < noOfCities - 1; i++)
            {
                int draw = toDraw[StaticRandom.Rand(toDraw.Count)];
                foundPath.Add(draw);
                toDraw.Remove(draw);
            }

            foundPath.Add(noOfCities - 1);
            return foundPath;
        }

        protected List<int> FindMinimumPathInListOfPaths(IEnumerable<List<int>> pathList, List<double> distances, int noOfCities)
        {
            double min = double.MaxValue;
            List<int> resultPath = new List<int>(noOfCities);

            foreach (var path in pathList)
            {
                if (path != null && path.Count == noOfCities)
                {
                    double distance = CalculateDistanceInPath(path, distances);
                    if (distance < min)
                    {
                        min = distance;
                        resultPath = path;
                    }
                }
            }
            return resultPath;
        }

        public double CalculateDistanceInPath(List<int> path, List<double> distances)
        {
            double result = 0;

            if (path != null)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    result += distances[path[i] * path.Count + path[i + 1]];
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

            public static double RandomDouble()
            {
                return Random.Value.NextDouble();
            }
        }
    }
}
