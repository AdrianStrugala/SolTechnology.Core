using DreamTravel.Infrastructure;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Trips.Queries.LimitCostOfPaths
{
    public class LimitCostOfPathsService : IService<LimitCostOfPathsInput, List<Path>>
    {
        public Task<List<Path>> Execute(LimitCostOfPathsInput input)
        {
            var paths = input.Paths;
            List<double> consideredVinietas = new List<double>();

            paths.Sort((x, y) => 1 * x.Goal.CompareTo(y.Goal));

            double overallCost = 0;
            foreach (var path in paths)
            {
                if (path.VinietaCost > 0)
                {
                    if (consideredVinietas.Contains(path.VinietaCost)) continue;
                    if (overallCost + path.VinietaCost > input.CostLimit)
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
                    if (overallCost + path.Cost <= input.CostLimit)
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

            return Task.FromResult(paths);
        }
    }
}
