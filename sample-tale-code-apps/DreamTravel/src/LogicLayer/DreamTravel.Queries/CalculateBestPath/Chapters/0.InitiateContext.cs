using DreamTravel.Domain.Cities;
using Hangfire.Annotations;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Queries.CalculateBestPath.Chapters;

[UsedImplicitly]
public class InitiateContext : Chapter<CalculateBestPathContext>
{
    public override Task<Result> Read(CalculateBestPathContext narration)
    {
         // Map CityDto to City domain model
         var cities = narration.Input.Cities
             .Select(dto => new City
             {
                 Name = dto.Name,
                 Latitude = dto.Latitude,
                 Longitude = dto.Longitude,
                 Country = dto.Country,
                 SearchStatistics = new List<CitySearchStatistics>(),
                 ReadOptions = CityReadOptions.Default
             })
             .ToList();

         narration.Cities = cities;
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