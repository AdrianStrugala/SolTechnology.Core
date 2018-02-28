using System;
using System.Collections.Generic;

namespace TSPTimeCost.TSP
{
    class God : TSP
    {
        public override void SolveTSP()
        {
            List<int> randomRoute = FindRandomRoute();

            UpdateBestPath(randomRoute, EvaluatedMatrix);
        }



        List<int> FindRandomRoute()
        {
            List<int> toDraw = new List<int>();
            List<int> foundPath = new List<int> {BestPath.Order[0]};
            Random ran = new Random();

            for (int i = 1; i < NoOfCities - 1; i++)
            {
                toDraw.Add(i);
            }

            while (foundPath.Count < NoOfCities - 1)
            {
                int draw = toDraw[ran.Next(toDraw.Count)];
                foundPath.Add(draw);
                toDraw.Remove(draw);
            }

            foundPath.Add(BestPath.Order[NoOfCities - 1]);

            return foundPath;
        }
    }
}