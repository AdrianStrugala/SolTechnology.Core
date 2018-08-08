using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection.Interfaces
{
    public interface IFormOutputDataForBestPath
    {
        List<Path> Execute(List<City> listOfCities, int[] orderOfCities, IEvaluationMatrix evaluationMatrix);
    }
}