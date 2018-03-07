using System;
using System.Collections.Concurrent;
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
        private ConcurrentBag<int[]> _paths;
        public override void SolveTSP()
        {
            //Stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int noOfUniverses = computionalPower / NoOfCities;
            _paths = new ConcurrentBag<int[]>();
            TSP.NoOfCities = Cities.Instance.ListOfCities.Count;

            Parallel.For(0, noOfUniverses, i => CreateUniverse());

            var minimumPath = FindMinimumPathInListOfPaths(_paths, EvaluatedMatrix);
            UpdateBestPath(minimumPath, EvaluatedMatrix);

            //Stopwatch
            BestPath.Instance.TimeOfExecution = stopwatch.Elapsed.ToString();
            stopwatch.Stop();
        }

        private void CreateUniverse()
        {
            int[] randomRoute = FindRandomRoute();
            try
            {
                _paths.Add(randomRoute);
            }
            catch (Exception ex)
            {
                BestPath.Instance.TimeOfExecution = ex.ToString();
            }
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
