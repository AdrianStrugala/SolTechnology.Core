using DreamTravel.TravelingSalesmanProblem;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Executors
{
    public interface ISolveTsp
    {
        Task<OperationResult> Execute(CalculateBestPathContext calculateBestPathContext);
    }

    public class SolveTsp : ISolveTsp
    {
        private readonly ITSP _tsp;

        public SolveTsp(ITSP tsp)
        {
            _tsp = tsp;
        }

        public Task<OperationResult> Execute(CalculateBestPathContext calculateBestPathContext)
        {
            calculateBestPathContext.OrderOfCities = _tsp.SolveTSP(calculateBestPathContext.OptimalDistances.ToList());
            return OperationResult.SucceededTask();
        }
    }
}
