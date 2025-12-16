using DreamTravel.Domain.Cities;
using DreamTravel.Ui.Models;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Ui.Services;

public interface ITspService
{
    Task<Result<City>> FindCityByNameAsync(string name);
    Task<Result<City>> FindCityByCoordinatesAsync(double lat, double lng);
    Task<Result<CalculateBestPathResult>> CalculateBestPathAsync(List<City> cities);
}
