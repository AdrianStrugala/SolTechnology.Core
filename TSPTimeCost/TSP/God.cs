using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{
    class God : TSP
    {
        private static readonly int computionalPower = 5000000;
        private List<int[]> _paths;
        public override void SolveTSP()
        {
            int noOfUniverses = computionalPower / NoOfCities;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            _paths = new List<int[]>();
            TSP.NoOfCities = Cities.Instance.ListOfCities.Count;

            Parallel.For(0, noOfUniverses, calc => CreateUniverse());

            int[] minimumPath = new int[NoOfCities];
            minimumPath = _paths[FindMinimumPathInListOfPaths(_paths, EvaluatedMatrix)];
            UpdateBestPath(minimumPath, EvaluatedMatrix);

            var s = stopwatch.Elapsed;
            stopwatch.Stop();
        }

        private void CreateUniverse()
        {
            int[] randomRoute = FindRandomRoute();
            _paths.Add(randomRoute);
        }

        int[] FindRandomRoute()
        {
            List<int> toDraw = new List<int>();


            int[] foundPath = new int[NoOfCities];
            for (int i = 0; i < NoOfCities; i++) { foundPath[i] = -1; }
            foundPath[0] = BestPath.Order[0];

           

            for (int i = 1; i < NoOfCities - 1; i++)
            {
                toDraw.Add(i);
            }

            for (int i = 1; i < NoOfCities - 1; i++)
            {
                int draw = toDraw[StaticRandom.Rand(toDraw.Count)];
                foundPath[i] = draw;
                toDraw.Remove(draw);
            }

            foundPath[NoOfCities - 1] = BestPath.Order[NoOfCities - 1];

            return foundPath;
        }
    }

    public static class StaticRandom
    {
        static int seed = Environment.TickCount;

        static readonly ThreadLocal<Random> random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        public static int Rand(int max)
        {
            return random.Value.Next(max);
        }
    }
}
