namespace DreamTravel.WebUI.LocationOfCity.Interfaces
{
    using System.Threading.Tasks;
    using SharedModels;

    public interface IFindLocationOfCity
    {
        Task<City> Execute(string cityName);
    }
}