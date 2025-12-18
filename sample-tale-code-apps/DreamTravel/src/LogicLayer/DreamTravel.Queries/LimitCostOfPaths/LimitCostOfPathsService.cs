using SolTechnology.Core.CQRS;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Queries.LimitCostOfPaths
{
    public class LimitCostOfPathsService : IQueryHandler<LimitCostOfPathsQuery, List<Path>>
    {
        public Task<Result<List<Path>>> Handle(LimitCostOfPathsQuery query, CancellationToken cancellationToken)
        {
            var paths = query.Paths;
            List<double> consideredVinietas = new List<double>();

            paths.Sort((x, y) => 1 * x.Goal.CompareTo(y.Goal));

            double overallCost = 0;
            foreach (var path in paths)
            {
                if (path.VinietaCost > 0)
                {
                    if (consideredVinietas.Contains(path.VinietaCost)) continue;
                    if (overallCost + path.VinietaCost > query.CostLimit)
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
                    if (overallCost + path.Cost <= query.CostLimit)
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

            return Result<List<Path>>.SuccessAsTask(paths);
        }
    }
}
