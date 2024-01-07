using DreamTravel.TravelingSalesmanProblem;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Executors
{
    public interface ISolveTsp
    {
        void Execute(CalculateBestPathContext calculateBestPathContext);
    }

    public class SolveTsp : ISolveTsp
    {
        private readonly ITSP _tsp;

        public SolveTsp(ITSP tsp)
        {
            _tsp = tsp;
        }

        public void Execute(CalculateBestPathContext calculateBestPathContext)
        {
            calculateBestPathContext.OrderOfCities = _tsp.SolveTSP(calculateBestPathContext.OptimalDistances.ToList());
        }
    }
}
