using System;
using System.Collections.Generic;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers.Interfaces;

namespace DreamTravel.TSPControllerHandlers
{
    public class BreakCostLimit : IBreakCostLimit
    {
        private const double ConversionError = 0.000001;
        public List<Path> Execute(int costLimit, List<Path> paths)
        {
            paths.Sort((x, y) => 1 * x.Goal.CompareTo(y.Goal));

            double overallCost = 0;
            foreach (var path in paths)
            {
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
