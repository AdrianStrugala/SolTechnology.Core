using DreamTravel.TravelingSalesmanProblem;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Queries.CalculateBestPath.Chapters;

public class SolveTsp : Chapter<CalculateBestPathContext>
{
    private readonly ITSP _tsp;

    public SolveTsp(ITSP tsp)
    {
        _tsp = tsp;
    }

    public override Task<Result> Read(CalculateBestPathContext calculateBestPathContext)
    {
        calculateBestPathContext.OrderOfCities = _tsp.SolveTSP(calculateBestPathContext.OptimalDistances.ToList());
        return Result.SuccessAsTask();
    }
}