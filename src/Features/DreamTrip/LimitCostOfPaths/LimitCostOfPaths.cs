using System.Collections.Generic;
using System.Linq;
using DreamTravel.Domain.Paths;

namespace DreamTravel.Features.DreamTrip.LimitCostOfPaths
{
    public class LimitCostOfPaths : ILimitCostOfPaths
    {
        public List<Path> Execute(int costLimit, List<Path> paths)
        {
            List<double> consideredVinietas = new List<double>();

            paths.Sort((x, y) => 1 * x.Goal.CompareTo(y.Goal));

            double overallCost = 0;
            foreach (var path in paths)
            {
                if (path.VinietaCost > 0)
                {
                    if (consideredVinietas.Contains(path.VinietaCost)) continue;
                    if (overallCost + path.VinietaCost > costLimit)
                    {
                        paths.Where(x => x.VinietaCost.Equals(path.VinietaCost)).ToList()
                            .ForEach(y =>
                            {
                                y.OptimalCost = 0;
                                y.OptimalDistance = y.FreeDistance;
                            });
                    }
                    else
                    {
                        overallCost += path.VinietaCost;

                        paths.Where(x => x.VinietaCost.Equals(path.VinietaCost)).ToList()
                            .ForEach(y =>
                            {
                                y.OptimalCost = y.Cost;
                                y.OptimalDistance = y.TollDistance;
                            });
                    }
                    consideredVinietas.Add(path.VinietaCost);
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
