namespace DreamTravel.WebUI.LocationOfCity.Interfaces
{
    using System.Threading.Tasks;
    using Contract;

    public interface IFindLocationOfCity
    {
        Task<City> Execute(string cityName);
    }
}