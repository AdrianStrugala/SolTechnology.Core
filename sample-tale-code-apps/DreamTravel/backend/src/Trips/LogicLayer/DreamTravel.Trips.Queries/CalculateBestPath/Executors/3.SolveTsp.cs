using DreamTravel.TravelingSalesmanProblem;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.SuperChain;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Executors;

public class SolveTsp : IChainStep<CalculateBestPathContext>
{
    private readonly ITSP _tsp;

    public SolveTsp(ITSP tsp)
    {
        _tsp = tsp;
    }

    public Task<Result> Execute(CalculateBestPathContext calculateBestPathContext)
    {
        calculateBestPathContext.OrderOfCities = _tsp.SolveTSP(calculateBestPathContext.OptimalDistances.ToList());
        return Result.SuccessAsTask();
    }
}