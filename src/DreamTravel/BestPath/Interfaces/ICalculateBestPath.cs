namespace DreamTravel.BestPath.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SharedModels;

    public interface ICalculateBestPath
    {
        Task<Result> Execute(List<City> cities, bool optimizePath);
    }
}