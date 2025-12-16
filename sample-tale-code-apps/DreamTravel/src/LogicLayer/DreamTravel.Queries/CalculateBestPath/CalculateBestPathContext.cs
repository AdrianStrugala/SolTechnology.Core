using DreamTravel.Domain.Cities;
using SolTechnology.Core.CQRS.SuperChain;

namespace DreamTravel.Queries.CalculateBestPath;

public sealed class CalculateBestPathContext : ChainContext<CalculateBestPathQuery, CalculateBestPathResult>
{
    public List<City> Cities { get; set; }
    public int NoOfCities { get; set; }
    public List<int> OrderOfCities { get; set; }

    public double[] FreeDistances { get; set; }
    public double[] TollDistances { get; set; }
    public double[] OptimalDistances { get; set; }
    public double[] Goals { get; set; }
    public double[] Costs { get; set; }
    public double[] OptimalCosts { get; set; }
    public double[] VinietaCosts { get; set; }
}