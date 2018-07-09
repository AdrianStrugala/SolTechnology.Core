using System;
using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.TSPControllerHandlers
{
    public class CostLimitBreaker : ICostLimitBreaker
    {
        private double conversionError = 0.000001;
        public List<Path> AdjustPaths(int costLimit, List<Path> paths)
        {
            paths.Sort((x, y) => 1 * x.Goal.CompareTo(y.Goal));

            double overallCost = 0;
            foreach (var path in paths)
            {
                if (Math.Abs(path.Goal) < conversionError) continue;
                if (overallCost + path.Cost <= costLimit)
                {
                    overallCost += path.Cost;
                    path.OptimalCost = path.Cost;
                    path.OptimalDistance = path.TollDistance;
                }
                else
                {
                    path.OptimalCost = 0;
                    path.OptimalDistance = path.FreeDistance;
                }
            }

            paths.Sort((x, y) => 1 * x.Index.CompareTo(y.Index));

            return paths;
        }
    }
}
