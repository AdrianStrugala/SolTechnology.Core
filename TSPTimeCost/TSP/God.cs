using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{
    class God : TSP
    {
        private int noOfUniverses = 200;
        private List<int[]> _paths;
        public override void SolveTSP()
        {
            _paths = new List<int[]>();
            TSP.NoOfCities = Cities.Instance.ListOfCities.Count;

            Parallel.For(0, noOfUniverses, calc => CreateUniverse());

            int[] minimumPath = _paths[FindMinimumPathInListOfPaths(_paths, EvaluatedMatrix)];
            UpdateBestPath(minimumPath, EvaluatedMatrix);
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

            Random ran = new Random();

            for (int i = 1; i < NoOfCities - 1; i++)
            {
                toDraw.Add(i);
            }

            for (int i = 1; i < NoOfCities - 1; i++)
            {
                int draw = toDraw[ran.Next(toDraw.Count)];
                foundPath[i] = draw;
                toDraw.Remove(draw);
            }

            foundPath[NoOfCities - 1] = BestPath.Order[NoOfCities - 1];

            return foundPath;
        }
    }
}