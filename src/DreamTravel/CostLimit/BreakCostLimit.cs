using System.Collections.Generic;
using System.Linq;
using DreamTravel.CostLimit.Interfaces;
using DreamTravel.SharedModels;

namespace DreamTravel.CostLimit
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
                if (path.VinietaCost > 0)
                {
                    if (!(overallCost + path.VinietaCost <= costLimit)) continue;
                    overallCost += path.VinietaCost;

                    paths.Where(x => x.VinietaCost.Equals(path.VinietaCost)).ToList()
                        .ForEach(y =>
                        {
                            y.OptimalCost = y.Cost;
                            y.OptimalDistance = y.TollDistance;
                        });
                }
                else
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
            }

            paths.Sort((x, y) => 1 * x.Index.CompareTo(y.Index));

            return paths;
        }
    }
}
