namespace DreamTravel.WebUI.BestPath.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Contract;
    using Models;

    public interface IDownloadRoadData
    {
        Task<EvaluationMatrix> Execute(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix);
    }
}