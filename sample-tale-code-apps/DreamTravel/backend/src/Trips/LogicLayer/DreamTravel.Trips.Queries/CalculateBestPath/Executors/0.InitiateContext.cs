using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.SuperChain;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Executors;

public class InitiateContext : IChainStep<CalculateBestPathContext>
{
    public Task<Result> Execute(CalculateBestPathContext context)
    {
         var cities = context.Input.Cities.Where(c => c != null).ToList();
         
         context.Cities = cities!;
         context.NoOfCities = cities.Count;
         int matrixSize = cities.Count * cities.Count;

         context.FreeDistances = new double[matrixSize];
         context.TollDistances = new double[matrixSize];
         context.OptimalDistances = new double[matrixSize];
         context.Goals = new double[matrixSize];
         context.Costs = new double[matrixSize];
         context.OptimalCosts = new double[matrixSize];
         context.VinietaCosts = new double[matrixSize];

        return Result.SuccessAsTask();
    }
}