using System.Collections.Generic;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public interface IFormOutputDataForBestPath
    {
        List<Path> Execute(List<City> listOfCities, int[] orderOfCities, IEvaluationMatrix evaluationMatrix);
    }
}