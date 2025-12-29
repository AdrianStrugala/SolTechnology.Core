using DreamTravel.TravelingSalesmanProblem;
using JetBrains.Annotations;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Queries.CalculateBestPath.Chapters;

[UsedImplicitly]
public class SolveTsp(ITSP tsp) : Chapter<CalculateBestPathContext>
{
    public override Task<Result> Read(CalculateBestPathContext calculateBestPathContext)
    {
        calculateBestPathContext.OrderOfCities = tsp.SolveTSP(calculateBestPathContext.OptimalDistances.ToList());
        return Result.SuccessAsTask();
    }
}