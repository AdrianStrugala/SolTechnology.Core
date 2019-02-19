namespace DreamTravel.WebUI.BestPath.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Models;
    using SharedModels;

    public interface IDownloadRoadData
    {
        Task<EvaluationMatrix> Execute(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix);
    }
}