using Hangfire.Annotations;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Queries.CalculateBestPath.Chapters;

[UsedImplicitly]
public class InitiateContext : Chapter<CalculateBestPathContext>
{
    public override Task<Result> Read(CalculateBestPathContext narration)
    {
         var cities = narration.Input.Cities.Where(c => c != null).ToList();

         narration.Cities = cities!;
         narration.NoOfCities = cities.Count;
         int matrixSize = cities.Count * cities.Count;

         narration.FreeDistances = new double[matrixSize];
         narration.TollDistances = new double[matrixSize];
         narration.OptimalDistances = new double[matrixSize];
         narration.Goals = new double[matrixSize];
         narration.Costs = new double[matrixSize];
         narration.OptimalCosts = new double[matrixSize];
         narration.VinietaCosts = new double[matrixSize];

        return Result.SuccessAsTask();
    }
}