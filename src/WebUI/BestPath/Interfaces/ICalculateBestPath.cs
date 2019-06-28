namespace DreamTravel.WebUI.BestPath.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Contract;

    public interface ICalculateBestPath
    {
        Task<Result> Execute(List<City> cities, bool optimizePath);
    }
}