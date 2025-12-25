using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Queries.CalculateBestPath.Chapters;

public class InitiateContext : Chapter<CalculateBestPathNarration>
{
    public override Task<Result> Read(CalculateBestPathNarration narration)
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